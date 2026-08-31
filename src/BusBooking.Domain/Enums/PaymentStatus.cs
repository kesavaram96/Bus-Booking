namespace BusBooking.Domain.Enums;

/// <summary>
/// Full set as specified for the Payment module. Only Pending, Paid, Failed and Cancelled are
/// reachable from this phase — Refunded/PartiallyRefunded follow cancellation/refund handling
/// (Phase 17), used from the start so this column never needs a widening migration later, the
/// same reasoning applied to BookingStatus in Phase 12.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
