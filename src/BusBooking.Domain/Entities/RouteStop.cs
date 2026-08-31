namespace BusBooking.Domain.Entities;

public class RouteStop : Common.BaseEntity
{
    public Guid RouteId { get; private set; }

    public string StopName { get; private set; } = default!;

    public int StopOrder { get; private set; }

    public TimeSpan? ExpectedArrivalTime { get; private set; }

    public TimeSpan? ExpectedDepartureTime { get; private set; }

    public bool AllowPickup { get; private set; }

    public bool AllowDropOff { get; private set; }

    private RouteStop()
    {
    }

    public RouteStop(
        Guid routeId,
        string stopName,
        int stopOrder,
        TimeSpan? expectedArrivalTime,
        TimeSpan? expectedDepartureTime,
        bool allowPickup,
        bool allowDropOff)
    {
        if (routeId == Guid.Empty)
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        SetStopName(stopName);
        SetStopOrder(stopOrder);
        ExpectedArrivalTime = expectedArrivalTime;
        ExpectedDepartureTime = expectedDepartureTime;
        AllowPickup = allowPickup;
        AllowDropOff = allowDropOff;
        RouteId = routeId;
    }

    public void UpdateDetails(
        string stopName,
        TimeSpan? expectedArrivalTime,
        TimeSpan? expectedDepartureTime,
        bool allowPickup,
        bool allowDropOff)
    {
        SetStopName(stopName);
        ExpectedArrivalTime = expectedArrivalTime;
        ExpectedDepartureTime = expectedDepartureTime;
        AllowPickup = allowPickup;
        AllowDropOff = allowDropOff;
    }

    public void UpdateOrder(int stopOrder) => SetStopOrder(stopOrder);

    private void SetStopName(string stopName)
    {
        if (string.IsNullOrWhiteSpace(stopName))
        {
            throw new ArgumentException("Stop name is required.", nameof(stopName));
        }

        StopName = stopName.Trim();
    }

    private void SetStopOrder(int stopOrder)
    {
        if (stopOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stopOrder), "Stop order must be greater than zero.");
        }

        StopOrder = stopOrder;
    }
}
