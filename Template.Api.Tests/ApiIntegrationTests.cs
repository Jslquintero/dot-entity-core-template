using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Template.Api.Models;
using Template.Data;
using Template.Data.Repository;
using Template.Model.Entities;
using Xunit;

namespace Template.Api.Tests;

public class ApiIntegrationTests
{
    /// <summary>Runs the public API against an isolated, migrated PostgreSQL database.</summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Api_PostgreSql_AuthenticatesAuthorizesAndRevokesSessions()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("Set TEST_POSTGRES_CONNECTION to a PostgreSQL account with CREATEDB permission. See README.");
        var databaseName = $"template_test_{Guid.NewGuid():N}";
        await using var server = new NpgsqlConnection(connection);
        await server.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", server))
            await create.ExecuteNonQueryAsync();
        var database = new NpgsqlConnectionStringBuilder(connection) { Database = databaseName }.ToString();
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__DefaultConnection"] = database,
            ["Jwt__Key"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
            ["Database__ApplyMigrations"] = "true",
            ["Seed__Enabled"] = "true",
            ["Seed__AdminEmail"] = "admin@example.test",
            ["Seed__AdminPassword"] = "Test-Admin-Password123!",
            ["Hangfire__Enabled"] = "false",
            ["HttpsRedirection__Enabled"] = "false"
        };
        var previous = settings.ToDictionary(pair => pair.Key, pair => Environment.GetEnvironmentVariable(pair.Key));
        try
        {
            foreach (var (key, value) in settings)
                Environment.SetEnvironmentVariable(key, value);
            await using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/healthz")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/users/logout", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsJsonAsync("/api/users/login", new { email = "invalid", password = "" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PostAsJsonAsync("/api/users/login", new { email = "admin@example.test", password = "wrong" })).StatusCode);

            var adminResponse = await client.PostAsJsonAsync("/api/users/login", new
            {
                email = "admin@example.test",
                password = settings["Seed__AdminPassword"]
            });
            Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
            var raw = await adminResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Set-Cookie", adminResponse.Headers.Select(header => header.Key));
            var admin = JsonSerializer.Deserialize<LoginResponse>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Contains("SysAdmin", admin.User.Roles);
            Assert.InRange(admin.TokenExpirationDate, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(16));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.Token);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
            var request = new { email = "user@example.test", password = "Test-User-Password123!", name = "Test", lastName = "User" };
            var created = await client.PostAsJsonAsync("/api/users", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var profile = (await created.Content.ReadFromJsonAsync<UserViewModel>())!;
            Assert.Equal(new[] { "User" }, profile.Roles);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/users", request)).StatusCode);

            client.DefaultRequestHeaders.Authorization = null;
            var login = await client.PostAsJsonAsync("/api/users/login", new { request.email, request.password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/users", request)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/users/logout", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
            client.DefaultRequestHeaders.Authorization = null;

            // A fresh scope checks actual persistence and repository error handling, not an in-memory substitute.
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
                Assert.Equal(2, await context.Users.CountAsync());
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<Role>>();
                Assert.Null(await repository.GetOneAsync(role => role.Name == "Missing"));
                await Assert.ThrowsAsync<DbUpdateException>(() => repository.InsertAsync(new Role
                {
                    Name = "duplicate",
                    NormalizedName = "SYSADMIN"
                }));
            }
            // Five incorrect passwords lock the account; the correct password must then fail too.
            for (var attempt = 0; attempt < 5; attempt++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/users/login",
                    new { request.email, password = "wrong" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/users/login",
                new { request.email, request.password })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/users/login",
                new { request.email, request.password })).StatusCode);

            // Startup is idempotent and does not recreate/reset the administrator.
            await Template.Api.Services.DatabaseInitializer.InitializeAsync(factory.Services, factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var user = (await users.FindByEmailAsync("admin@example.test"))!;
                user.IsActive = false;
                Assert.True((await users.UpdateAsync(user)).Succeeded);
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", admin.Token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        }
        finally
        {
            foreach (var (key, value) in previous)
                Environment.SetEnvironmentVariable(key, value);
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", server);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
