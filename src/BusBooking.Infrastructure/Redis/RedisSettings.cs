namespace BusBooking.Infrastructure.Redis;

/// <summary>Bound from the "Redis" configuration section.</summary>
public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = default!;

    /// <summary>Prefixed onto every key so this instance can safely be shared across environments/apps.</summary>
    public string InstanceName { get; set; } = "BusBooking:";
}
