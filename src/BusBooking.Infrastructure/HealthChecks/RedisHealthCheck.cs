using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BusBooking.Infrastructure.HealthChecks;

/// <summary>No third-party health-check package needed for this — it's a two-line PING against
/// the same IConnectionMultiplexer already registered for seat locking.</summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisHealthCheck(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _multiplexer.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responded in {latency.TotalMilliseconds:F0}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed.", ex);
        }
    }
}
