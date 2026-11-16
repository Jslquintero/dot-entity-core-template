using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Template.Api.Configuration;
using Template.Api.Services;
using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Template.Services;
using Template.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
    // Environment variables and command-line arguments take precedence over local settings.
    builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
}
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration).WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection for PostgreSQL.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IRoleServices, RoleServices>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddIdentityCore<User>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<Role>().AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager().AddDefaultTokenProviders();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName).ValidateDataAnnotations()
    .Validate(options => options.HasStrongKey(), "Jwt:Key must contain at least 32 UTF-8 bytes.")
    .ValidateOnStart();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
if (!jwt.HasStrongKey())
    throw new InvalidOperationException("Configure Jwt:Key with at least 32 UTF-8 bytes using secrets or environment variables.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            // Database validation makes logout, password changes and deactivation revoke existing tokens.
            if (user is null || !user.IsActive ||
                context.Principal?.FindFirstValue(JwtOptions.SecurityStampClaim) != user.SecurityStamp)
                context.Fail("The session is no longer valid.");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Template API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

// Memory storage is explicitly opt-in and development-only. Production jobs need durable storage.
var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled");
if (hangfireEnabled)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configure durable Hangfire storage before enabling jobs outside Development.");
    builder.Services.AddHangfire(configuration => configuration.UseMemoryStorage());
    builder.Services.AddHangfireServer();
}

var app = builder.Build();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (builder.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
    app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
if (hangfireEnabled)
    app.UseHangfireDashboard(); // Hangfire's default dashboard filter allows local requests only.
app.MapHealthChecks("/healthz");
app.MapControllers();

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration);
await app.RunAsync();

public partial class Program;
