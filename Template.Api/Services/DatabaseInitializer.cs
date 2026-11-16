using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Data;
using Template.Model.Entities;

namespace Template.Api.Services;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (configuration.GetValue<bool>("Database:ApplyMigrations"))
            await context.Database.MigrateAsync();

        if (!configuration.GetValue<bool>("Seed:Enabled"))
            return;

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Seeding requires Seed:AdminEmail and Seed:AdminPassword.");

        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        foreach (var name in new[] { "SysAdmin", "Admin", "User" })
        {
            if (!await roles.RoleExistsAsync(name))
                EnsureSuccess(await roles.CreateAsync(new Role { Name = name }));
        }
        // Never reset an existing account's password or elevate it on subsequent starts.
        if (await users.FindByEmailAsync(email) is not null)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync();
        var admin = new User
        {
            UserName = email,
            Email = email,
            Name = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true
        };
        EnsureSuccess(await users.CreateAsync(admin, password));
        EnsureSuccess(await users.AddToRoleAsync(admin, "SysAdmin"));
        await transaction.CommitAsync();
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer")
            .LogInformation("Initial administrator created.");
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
