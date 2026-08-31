namespace BusBooking.Domain.Common;

/// <summary>
/// Determines whether two journey segments on the same route — each expressed as a pickup/
/// drop-off stop order — overlap. A segment is the half-open range [pickupOrder, dropOffOrder):
/// a passenger occupies the seat from their pickup stop up to (but not including) their
/// drop-off stop, so two segments that merely touch at a shared stop (one's drop-off equals the
/// other's pickup) do NOT overlap and can share the same physical seat.
/// </summary>
public static class SegmentOverlap
{
    public static bool Overlaps(int firstPickupOrder, int firstDropOffOrder, int secondPickupOrder, int secondDropOffOrder) =>
        firstPickupOrder < secondDropOffOrder && secondPickupOrder < firstDropOffOrder;
}
