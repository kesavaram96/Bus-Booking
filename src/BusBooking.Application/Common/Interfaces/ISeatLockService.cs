namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// Coordinates temporary seat holds via Redis so two customers can never both grab the same
/// seat. Redis is the source of truth for *who currently holds the lock and for how long* —
/// TripSeat's Status/LockId/LockedUntil in the database mirror that for read purposes, but the
/// database never arbitrates the race itself. Safe for multiple API instances against one
/// shared Redis instance, since exclusivity comes from Redis's own atomicity guarantees, not
/// from any in-process state.
/// </summary>
public interface ISeatLockService
{
    /// <summary>Atomically acquires the lock only if no valid lock currently exists for this seat.</summary>
    Task<SeatLockAcquireResult> TryAcquireAsync(Guid tripSeatId, TimeSpan duration, CancellationToken cancellationToken);

    /// <summary>Atomically releases the lock only if <paramref name="lockId"/> matches the current holder.</summary>
    Task<SeatLockReleaseResult> ReleaseAsync(Guid tripSeatId, string lockId, CancellationToken cancellationToken);
}

public sealed record SeatLockAcquireResult(bool Acquired, string? LockId, DateTime? LockedUntil)
{
    public static SeatLockAcquireResult Success(string lockId, DateTime lockedUntil) => new(true, lockId, lockedUntil);

    public static readonly SeatLockAcquireResult Failed = new(false, null, null);
}

public enum SeatLockReleaseResult
{
    /// <summary>The lock existed, matched, and was deleted.</summary>
    Released,

    /// <summary>No lock existed under this seat — already expired or never locked. Treated as a safe no-op by callers.</summary>
    NotFound,

    /// <summary>A lock exists but under a different token — someone else legitimately holds it now.</summary>
    Mismatch
}
