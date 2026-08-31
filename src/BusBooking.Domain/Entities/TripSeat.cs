using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

/// <summary>
/// A seat's *lock* state for one specific trip — Available/Held/Blocked. Generated
/// automatically from the assigned bus's active seats whenever a trip is created or its bus
/// changes. Deliberately does not track "booked": since Phase 13, a seat can be booked for
/// multiple non-overlapping journey segments at once (see Domain.Common.SegmentOverlap), so a
/// single global "booked" flag on the seat itself would be actively wrong, not just imprecise —
/// whether a segment is available is answered by querying existing BookingPassenger rows for
/// this seat, not by a status field here.
/// </summary>
public class TripSeat : Common.BaseEntity
{
    public Guid TripId { get; private set; }

    public Guid SeatId { get; private set; }

    public Seat Seat { get; private set; } = default!;

    public TripSeatStatus Status { get; private set; }

    /// <summary>The Redis lock token currently holding this seat, if any.</summary>
    public string? LockId { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    private TripSeat()
    {
    }

    public TripSeat(Guid tripId, Guid seatId)
    {
        if (tripId == Guid.Empty)
        {
            throw new ArgumentException("Trip id is required.", nameof(tripId));
        }

        if (seatId == Guid.Empty)
        {
            throw new ArgumentException("Seat id is required.", nameof(seatId));
        }

        TripId = tripId;
        SeatId = seatId;
        Status = TripSeatStatus.Available;
    }

    public void Block()
    {
        if (Status != TripSeatStatus.Available)
        {
            throw new InvalidOperationException("Only an available seat can be blocked.");
        }

        Status = TripSeatStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status != TripSeatStatus.Blocked)
        {
            throw new InvalidOperationException("Only a blocked seat can be unblocked.");
        }

        Status = TripSeatStatus.Available;
    }

    /// <summary>
    /// Reflects a Redis lock already acquired atomically by the caller — real exclusivity is
    /// decided by Redis (SET NX), not by this guard. A stale/expired Held row is fine to
    /// overwrite; only a staff Block is rejected here.
    /// </summary>
    public void Hold(string lockId, DateTime lockedUntil)
    {
        if (Status == TripSeatStatus.Blocked)
        {
            throw new InvalidOperationException("A blocked seat cannot be held.");
        }

        if (string.IsNullOrWhiteSpace(lockId))
        {
            throw new ArgumentException("Lock id is required.", nameof(lockId));
        }

        Status = TripSeatStatus.Held;
        LockId = lockId;
        LockedUntil = lockedUntil;
    }

    /// <summary>Idempotent: releasing a seat that isn't Held (already released, expired and
    /// cleaned up elsewhere, etc.) is a safe no-op rather than an error.</summary>
    public void ReleaseHold()
    {
        if (Status != TripSeatStatus.Held)
        {
            return;
        }

        Status = TripSeatStatus.Available;
        LockId = null;
        LockedUntil = null;
    }
}
