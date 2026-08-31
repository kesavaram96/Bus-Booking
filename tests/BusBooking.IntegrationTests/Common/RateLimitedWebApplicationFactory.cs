using BusBooking.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BusBooking.IntegrationTests.Common;

/// <summary>
/// A separate WebApplicationFactory type, deliberately not derived from
/// CustomWebApplicationFactory: it needs a genuinely low login rate limit to prove 429s really
/// happen, while every other test class needs the opposite (a limit high enough that a full
/// suite run never trips it). Environment variables are process-wide, so the low limit is set
/// in the constructor and put back in Dispose — safe because the test host runs every test
/// class fully sequentially (xunit.runner.json), never interleaved with another class's own
/// factory construction.
/// </summary>
public sealed class RateLimitedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"BusBookingRateLimitTests-{Guid.NewGuid()}";

    public RateLimitedWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder:reloadConfigOnChange", "false");
        Environment.SetEnvironmentVariable("Hangfire__UseMemoryStorage", "true");
        Environment.SetEnvironmentVariable("RateLimiting__Global__PermitLimit", "1000000");
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", "2");
        Environment.SetEnvironmentVariable("RateLimiting__Login__WindowSeconds", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override void Dispose(bool disposing)
    {
        // Restored to the value CustomWebApplicationFactory expects — every other test class
        // built after this one still needs the login limit high enough to never trip.
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", "1000000");
        Environment.SetEnvironmentVariable("RateLimiting__Login__WindowSeconds", null);

        base.Dispose(disposing);
    }
}
