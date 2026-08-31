using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Trip : Common.BaseAuditableEntity
{
    public Guid RouteId { get; private set; }

    public Route Route { get; private set; } = default!;

    public Guid BusId { get; private set; }

    public Bus Bus { get; private set; } = default!;

    public DateOnly TripDate { get; private set; }

    public TimeSpan DepartureTime { get; private set; }

    public TimeSpan ExpectedArrivalTime { get; private set; }

    public Guid? DriverId { get; private set; }

    public Driver? Driver { get; private set; }

    public decimal Fare { get; private set; }

    public TripStatus Status { get; private set; }

    /// <summary>
    /// Trip departure as an absolute point in time.
    /// </summary>
    public DateTime DepartureDateTime => ComputeDepartureDateTime(TripDate, DepartureTime);

    /// <summary>
    /// Trip's expected arrival as an absolute point in time. If the arrival time-of-day is not
    /// after the departure time-of-day, the trip is assumed to arrive the following day
    /// (an overnight service, e.g. departs 8 PM, arrives 5 AM).
    /// </summary>
    public DateTime ExpectedArrivalDateTime => ComputeArrivalDateTime(TripDate, DepartureTime, ExpectedArrivalTime);

    private Trip()
    {
    }

    public Trip(
        Guid routeId,
        Guid busId,
        DateOnly tripDate,
        TimeSpan departureTime,
        TimeSpan expectedArrivalTime,
        Guid? driverId,
        decimal fare)
    {
        if (routeId == Guid.Empty)
        {
            throw new ArgumentException("Route id is required.", nameof(routeId));
        }

        if (busId == Guid.Empty)
        {
            throw new ArgumentException("Bus id is required.", nameof(busId));
        }

        SetFare(fare);

        RouteId = routeId;
        BusId = busId;
        TripDate = tripDate;
        DepartureTime = departureTime;
        ExpectedArrivalTime = expectedArrivalTime;
        DriverId = driverId;
        Status = TripStatus.Draft;
    }

    public static DateTime ComputeDepartureDateTime(DateOnly tripDate, TimeSpan departureTime) =>
        tripDate.ToDateTime(TimeOnly.MinValue) + departureTime;

    public static DateTime ComputeArrivalDateTime(DateOnly tripDate, TimeSpan departureTime, TimeSpan expectedArrivalTime)
    {
        var departure = ComputeDepartureDateTime(tripDate, departureTime);
        var arrival = tripDate.ToDateTime(TimeOnly.MinValue) + expectedArrivalTime;

        return arrival <= departure ? arrival.AddDays(1) : arrival;
    }

    public void UpdateSchedule(DateOnly tripDate, TimeSpan departureTime, TimeSpan expectedArrivalTime, decimal fare)
    {
        EnsureEditable();
        SetFare(fare);
        TripDate = tripDate;
        DepartureTime = departureTime;
        ExpectedArrivalTime = expectedArrivalTime;
    }

    public void AssignBus(Guid busId)
    {
        EnsureEditable();

        if (busId == Guid.Empty)
        {
            throw new ArgumentException("Bus id is required.", nameof(busId));
        }

        BusId = busId;
    }

    public void AssignDriver(Guid driverId)
    {
        EnsureEditable();

        if (driverId == Guid.Empty)
        {
            throw new ArgumentException("Driver id is required.", nameof(driverId));
        }

        DriverId = driverId;
    }

    public void RemoveDriver()
    {
        EnsureEditable();
        DriverId = null;
    }

    public void Schedule()
    {
        if (Status != TripStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft trip can be scheduled.");
        }

        Status = TripStatus.Scheduled;
    }

    public void MarkBoarding()
    {
        if (Status != TripStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a scheduled trip can be marked as boarding.");
        }

        Status = TripStatus.Boarding;
    }

    public void MarkDeparted()
    {
        if (Status != TripStatus.Boarding)
        {
            throw new InvalidOperationException("Only a boarding trip can be marked as departed.");
        }

        Status = TripStatus.Departed;
    }

    public void MarkCompleted()
    {
        if (Status != TripStatus.Departed)
        {
            throw new InvalidOperationException("Only a departed trip can be marked as completed.");
        }

        Status = TripStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
        {
            throw new InvalidOperationException("A completed or already cancelled trip cannot be cancelled.");
        }

        Status = TripStatus.Cancelled;
    }

    private void EnsureEditable()
    {
        if (Status is TripStatus.Departed or TripStatus.Completed or TripStatus.Cancelled)
        {
            throw new InvalidOperationException("This trip can no longer be modified.");
        }
    }

    private void SetFare(decimal fare)
    {
        if (fare <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fare), "Fare must be greater than zero.");
        }

        Fare = fare;
    }
}
