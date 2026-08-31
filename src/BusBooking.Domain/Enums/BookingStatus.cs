namespace BusBooking.Domain.Enums;

/// <summary>
/// Full set as specified for Phase 17 (Cancellation), used from the start so Booking's status
/// column never needs a widening migration later. Only Pending is reachable from this phase —
/// Confirmed follows payment (Phase 14), Cancelled/NoShow/Refunded follow cancellation (Phase 17).
/// </summary>
public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,
    NoShow = 5,
    Refunded = 6
}
