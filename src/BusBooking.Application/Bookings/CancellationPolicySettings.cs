namespace BusBooking.Application.Bookings;

/// <summary>
/// Bound from the "CancellationPolicy" configuration section — the doc's "create configurable
/// cancellation rules." Applies only to customer/guest self-cancellation; business staff can
/// always cancel a booking up until its trip is completed, regardless of this window.
/// </summary>
public class CancellationPolicySettings
{
    public const string SectionName = "CancellationPolicy";

    public int MinimumHoursBeforeDeparture { get; set; } = 2;
}
