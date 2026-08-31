using BusBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BusBooking.Infrastructure.Redis;

public class RedisSeatLockService : ISeatLockService
{
    // The classic safe-release pattern (see redis.io's distributed locks page): a plain
    // GET-then-DEL has a race between the two calls, so releasing must be one atomic script
    // that only deletes when the value still matches the caller's own lock token.
    private const string ReleaseScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end
        """;

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisSettings _settings;

    public RedisSeatLockService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisSettings> settings)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _settings = settings.Value;
    }

    public async Task<SeatLockAcquireResult> TryAcquireAsync(
        Guid tripSeatId,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var key = GetKey(tripSeatId);
        var lockId = Guid.CreateVersion7().ToString("N");

        // SET key value NX EX — a single atomic command, so two concurrent requests for the
        // same seat can never both succeed regardless of how many API instances issue them.
        var acquired = await database.StringSetAsync(key, lockId, duration, When.NotExists);

        return acquired
            ? SeatLockAcquireResult.Success(lockId, DateTime.UtcNow.Add(duration))
            : SeatLockAcquireResult.Failed;
    }

    public async Task<SeatLockReleaseResult> ReleaseAsync(
        Guid tripSeatId,
        string lockId,
        CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var key = GetKey(tripSeatId);

        var result = (long)await database.ScriptEvaluateAsync(ReleaseScript, [key], [lockId]);
        if (result == 1)
        {
            return SeatLockReleaseResult.Released;
        }

        // Only used to pick the friendlier error message below — the script above is what
        // actually decided, atomically, whether anything was deleted.
        var stillExists = await database.KeyExistsAsync(key);
        return stillExists ? SeatLockReleaseResult.Mismatch : SeatLockReleaseResult.NotFound;
    }

    private string GetKey(Guid tripSeatId) => $"{_settings.InstanceName}seatlock:{tripSeatId}";
}
