using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Trips;

public class TripSeatTests
{
    [Fact]
    public void Constructor_StartsAvailable()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        tripSeat.Status.Should().Be(TripSeatStatus.Available);
    }

    [Fact]
    public void Hold_FromAvailable_TransitionsToHeld()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        tripSeat.Hold("lock-token", DateTime.UtcNow.AddMinutes(10));

        tripSeat.Status.Should().Be(TripSeatStatus.Held);
        tripSeat.LockId.Should().Be("lock-token");
        tripSeat.LockedUntil.Should().NotBeNull();
    }

    [Fact]
    public void Hold_WithEmptyLockId_Throws()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        var act = () => tripSeat.Hold(" ", DateTime.UtcNow.AddMinutes(10));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Hold_WhenBlocked_Throws()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());
        tripSeat.Block();

        var act = () => tripSeat.Hold("lock-token", DateTime.UtcNow.AddMinutes(10));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReleaseHold_FromHeld_ReturnsToAvailableAndClearsLock()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());
        tripSeat.Hold("lock-token", DateTime.UtcNow.AddMinutes(10));

        tripSeat.ReleaseHold();

        tripSeat.Status.Should().Be(TripSeatStatus.Available);
        tripSeat.LockId.Should().BeNull();
        tripSeat.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void ReleaseHold_WhenNotHeld_IsIdempotentNoOp()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        var act = () => tripSeat.ReleaseHold();

        act.Should().NotThrow();
        tripSeat.Status.Should().Be(TripSeatStatus.Available);
    }

    [Fact]
    public void Block_FromAvailable_TransitionsToBlocked()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        tripSeat.Block();

        tripSeat.Status.Should().Be(TripSeatStatus.Blocked);
    }

    [Fact]
    public void Block_WhenAlreadyBlocked_Throws()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());
        tripSeat.Block();

        var act = () => tripSeat.Block();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unblock_FromBlocked_TransitionsToAvailable()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());
        tripSeat.Block();

        tripSeat.Unblock();

        tripSeat.Status.Should().Be(TripSeatStatus.Available);
    }

    [Fact]
    public void Unblock_WhenNotBlocked_Throws()
    {
        var tripSeat = new TripSeat(Guid.NewGuid(), Guid.NewGuid());

        var act = () => tripSeat.Unblock();

        act.Should().Throw<InvalidOperationException>();
    }
}
