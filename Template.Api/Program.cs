
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Api.Middlewares;
using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Globalization;
using Template.Api.Utils;
using Hangfire;
// Identity seeding method
static async Task SeedIdentityData(UserManager<User> userManager, RoleManager<Role> roleManager)
{
    // Seed Roles
    var roles = new[] { "SysAdmin", "Admin", "Reparacion", "Caja" };

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var role = new Role { Name = roleName };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                Log.Error("Failed to create role {RoleName}: {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    // Seed Admin User
    const string adminEmail = "admin@admin.com";
    const string adminPassword = "Admin1234";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = "System",
            LastName = "Administrator",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            // Assign SysAdmin role
            await userManager.AddToRoleAsync(adminUser, "SysAdmin");
            Log.Information("Admin user created successfully with email: {Email}", adminEmail);
        }
        else
        {
            Log.Error("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    else
    {
        // Ensure admin user has SysAdmin role
        if (!await userManager.IsInRoleAsync(adminUser, "SysAdmin"))
        {
            await userManager.AddToRoleAsync(adminUser, "SysAdmin");
            Log.Information("SysAdmin role assigned to existing admin user");
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)); // Can be changed to UseNpgsql() for PostgreSQL

// Serilog Logging Configuration
// For database logging, install appropriate sink package:
// For SQL Server: Serilog.Sinks.MSSqlServer
// For PostgreSQL: Serilog.Sinks.PostgreSQL
// Then configure: .WriteTo.MSSqlServer(...) or .WriteTo.PostgreSQL(...)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Repository Services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Identity Configuration
builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;

    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
});

// Identity Services
builder.Services.AddTransient<UserManager<User>>();
builder.Services.AddTransient<RoleManager<Role>>();

// JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration.GetValue<string>("SecretKey") ?? "default-secret-key");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(AutoMapping));

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "_MyCors", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Configure as needed for your origins
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()); // Uses in-memory storage by default for development

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 10); // Dynamic worker count
    options.Queues = new[] { "default", "critical" }; // Define queues
});

// Service Registration Patterns
// Register services following these patterns:
// builder.Services.AddScoped<IServiceName, ServiceName>();
// builder.Services.AddTransient<IEmailServices, EmailServices>();
// builder.Services.AddSingleton<ISharedService, SharedService>();

// Background Services Structure (optional)
// To add background services, create classes that implement IHostedService:
// public class YourBackgroundService : BackgroundService
// {
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         // Your background processing logic here
//     }
// }
// Then register: builder.Services.AddHostedService<YourBackgroundService>();

var app = builder.Build();

// Database Migration and Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    try
    {
        Log.Information("Applying database migrations...");
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully.");

        // Seed identity data
        await SeedIdentityData(userManager, roleManager);
        Log.Information("Identity data seeded successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database migration or seeding");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("_MyCors");

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseApplicationIdentity();

app.UseHealthChecks("/healthz");

app.MapControllers();

app.Run();