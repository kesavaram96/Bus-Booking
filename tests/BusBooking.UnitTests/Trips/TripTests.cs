using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Trips;

public class TripTests
{
    private static Trip CreateTrip(
        DateOnly? tripDate = null,
        TimeSpan? departureTime = null,
        TimeSpan? expectedArrivalTime = null) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tripDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            departureTime ?? TimeSpan.FromHours(8),
            expectedArrivalTime ?? TimeSpan.FromHours(17),
            null,
            fare: 3500m);

    [Fact]
    public void Constructor_WithValidData_StartsInDraftStatus()
    {
        var trip = CreateTrip();

        trip.Status.Should().Be(TripStatus.Draft);
    }

    [Fact]
    public void Constructor_WithNonPositiveFare_Throws()
    {
        var act = () => new Trip(
            Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), TimeSpan.Zero, TimeSpan.FromHours(1), null, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExpectedArrivalDateTime_WhenArrivalAfterDepartureSameDay_DoesNotRollOver()
    {
        var tripDate = new DateOnly(2026, 9, 1);
        var trip = CreateTrip(tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        trip.ExpectedArrivalDateTime.Should().Be(new DateTime(2026, 9, 1, 17, 0, 0));
    }

    [Fact]
    public void ExpectedArrivalDateTime_ForOvernightTrip_RollsOverToNextDay()
    {
        // Doc's own example: departs 8 PM, arrives 5 AM the next day.
        var tripDate = new DateOnly(2026, 9, 1);
        var trip = CreateTrip(tripDate, TimeSpan.FromHours(20), TimeSpan.FromHours(5));

        trip.ExpectedArrivalDateTime.Should().Be(new DateTime(2026, 9, 2, 5, 0, 0));
    }

    [Fact]
    public void FullLifecycle_DraftToCompleted_TransitionsInOrder()
    {
        var trip = CreateTrip();

        trip.Schedule();
        trip.Status.Should().Be(TripStatus.Scheduled);

        trip.MarkBoarding();
        trip.Status.Should().Be(TripStatus.Boarding);

        trip.MarkDeparted();
        trip.Status.Should().Be(TripStatus.Departed);

        trip.MarkCompleted();
        trip.Status.Should().Be(TripStatus.Completed);
    }

    [Fact]
    public void MarkBoarding_WhenStillDraft_Throws()
    {
        var trip = CreateTrip();

        var act = trip.MarkBoarding;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromDraft_Succeeds()
    {
        var trip = CreateTrip();

        trip.Cancel();

        trip.Status.Should().Be(TripStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCompleted_Throws()
    {
        var trip = CreateTrip();
        trip.Schedule();
        trip.MarkBoarding();
        trip.MarkDeparted();
        trip.MarkCompleted();

        var act = trip.Cancel;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateSchedule_WhenDeparted_Throws()
    {
        var trip = CreateTrip();
        trip.Schedule();
        trip.MarkBoarding();
        trip.MarkDeparted();

        var act = () => trip.UpdateSchedule(trip.TripDate, trip.DepartureTime, trip.ExpectedArrivalTime, 4000m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssignDriver_ThenRemoveDriver_ClearsDriverId()
    {
        var trip = CreateTrip();
        var driverId = Guid.NewGuid();

        trip.AssignDriver(driverId);
        trip.DriverId.Should().Be(driverId);

        trip.RemoveDriver();
        trip.DriverId.Should().BeNull();
    }
}
