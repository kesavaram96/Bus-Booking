using System.Net.Http.Json;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Infrastructure.Identity;
using BusBooking.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BusBooking.IntegrationTests.Common;

/// <summary>
/// Swaps the SQL Server-backed ApplicationDbContext for an EF Core InMemory one (unique per
/// factory instance, so test classes never share data), so the test host — including identity
/// role/seed startup logic — never needs a live SQL Server.
/// EF Core registers provider config as IDbContextOptionsConfiguration&lt;TContext&gt; (to support
/// chaining multiple AddDbContext calls), so that has to be removed too — removing only
/// DbContextOptions&lt;TContext&gt; leaves the SQL Server provider config active alongside
/// InMemory's and EF throws "only a single database provider can be registered".
/// Also points Hangfire at an in-memory job store (Hangfire.MemoryStorage) instead of SQL
/// Server, the same swap-for-tests idea, and email delivery at a per-instance pickup directory
/// instead of a real SMTP server.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    static CustomWebApplicationFactory()
    {
        // Every WebApplicationFactory host otherwise opens a FileSystemWatcher (inotify) per
        // appsettings*.json file it loads with the ASP.NET Core default of reloadOnChange:true —
        // pointless for a test host that never edits its own config on disk, and on a dev
        // machine already running many other inotify-backed tools (IDEs, desktop indexers) that
        // can exhaust the low per-user default (`fs.inotify.max_user_instances`, commonly 128)
        // just from running this test suite's many WebApplicationFactory instances. Disabling
        // it here (before any host is built) removes that watcher entirely.
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder:reloadConfigOnChange", "false");

        // Program.cs reads Hangfire:UseMemoryStorage synchronously, inline, while building
        // services — well before ConfigureWebHost's ConfigureAppConfiguration callback (below)
        // gets a chance to run. An environment variable is loaded as part of
        // WebApplication.CreateBuilder(args) itself, so it's visible from the very first line
        // of Program.cs, unlike a source added via ConfigureAppConfiguration. Without this, the
        // test host would try to open a real SQL Server connection for Hangfire's job storage.
        Environment.SetEnvironmentVariable("Hangfire__UseMemoryStorage", "true");

        // Phase 20's rate limiting is genuine and active in every test host (nothing here
        // disables the feature) — but every test in this suite runs from the same loopback
        // "client IP" as every other, and a full run makes far more than 300 requests/minute
        // (or 5 login calls/minute — CreateBusinessUserAndGetAccessTokenAsync alone calls
        // /api/auth/login once per test needing a staff token) in total. Raised here so ordinary
        // functional tests never trip it; RateLimitingTests.cs proves the real, low-limit
        // behavior for real using its own separate WebApplicationFactory type instead, so its
        // static constructor's environment variables don't collide with this one's.
        Environment.SetEnvironmentVariable("RateLimiting__Global__PermitLimit", "1000000");
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", "1000000");
    }

    private readonly string _databaseName = $"BusBookingIntegrationTests-{Guid.NewGuid()}";

    public string EmailPickupDirectory { get; } = Path.Combine(Path.GetTempPath(), $"BusBookingTests-Email-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:PickupDirectory"] = EmailPickupDirectory
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>
    /// Directly provisions a business user in the given role (there is no self-registration
    /// endpoint for business roles, matching the real system) and logs in through the real
    /// /api/auth/login endpoint so the returned token carries genuine, DB-driven role claims.
    /// </summary>
    public async Task<string> CreateBusinessUserAndGetAccessTokenAsync(string role, string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@example.com";
        const string password = "P@ssw0rd123";

        using (var scope = Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Test User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, role);
        }

        using var client = CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = email, password });
        var body = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();

        return body!.Data!.AccessToken;
    }
}
