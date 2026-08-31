using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.SeatLayouts;

public class SeatTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesActiveSeat()
    {
        var seat = new Seat(Guid.NewGuid(), " 12 ", row: 2, column: 1, SeatPositionType.Seat);

        seat.SeatNumber.Should().Be("12");
        seat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptySeatLayoutId_Throws()
    {
        var act = () => new Seat(Guid.Empty, "01", 0, 0, SeatPositionType.Seat);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var seat = new Seat(Guid.NewGuid(), "01", 0, 0, SeatPositionType.Seat);

        seat.Deactivate();

        seat.IsActive.Should().BeFalse();
    }
}
