using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BusBooking.API.Extensions;

/// <summary>
/// Built into ASP.NET Core (Microsoft.AspNetCore.RateLimiting) — no third-party package needed.
/// Two policies: a generous global default (protects the app from being overwhelmed at all),
/// and a much stricter one for login specifically (the doc's classic "prevent brute-force
/// credential guessing" case), applied via [EnableRateLimiting("login")] on that one action.
/// Partitioned by client IP, not by authenticated user, since the most important case —
/// unauthenticated login attempts — has no user identity to partition by yet.
///
/// Both limits are configuration-driven (RateLimiting:Global/Login) rather than hardcoded,
/// specifically so the integration test host can raise them far above what a full test run's
/// request volume would otherwise trip — the same "swap it for tests" idea already applied to
/// Hangfire's job storage.
/// </summary>
public static class RateLimitingServiceExtensions
{
    public const string LoginPolicy = "login";

    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPermitLimit = configuration.GetValue("RateLimiting:Global:PermitLimit", 300);
        var globalWindowSeconds = configuration.GetValue("RateLimiting:Global:WindowSeconds", 60);
        var loginPermitLimit = configuration.GetValue("RateLimiting:Login:PermitLimit", 5);
        var loginWindowSeconds = configuration.GetValue("RateLimiting:Login:WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(globalWindowSeconds),
                        PermitLimit = globalPermitLimit,
                        QueueLimit = 0
                    }));

            options.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(loginWindowSeconds),
                        PermitLimit = loginPermitLimit,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string ClientKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
