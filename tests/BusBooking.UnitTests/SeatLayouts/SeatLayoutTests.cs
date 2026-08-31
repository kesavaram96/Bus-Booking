using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.SeatLayouts;

public class SeatLayoutTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesLayout()
    {
        var layout = new SeatLayout("49-Seater Standard", "Default layout", rows: 13, columns: 4);

        layout.Rows.Should().Be(13);
        layout.Columns.Should().Be(4);
        layout.Seats.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(13, 0)]
    [InlineData(-1, 4)]
    public void Constructor_WithNonPositiveDimensions_Throws(int rows, int columns)
    {
        var act = () => new SeatLayout("Layout", null, rows, columns);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddSeat_AddsSeatToCollection()
    {
        var layout = new SeatLayout("Layout", null, 13, 4);
        var seat = new Seat(layout.Id, "01", row: 0, column: 0, SeatPositionType.Seat);

        layout.AddSeat(seat);

        layout.Seats.Should().ContainSingle().Which.Should().Be(seat);
    }
}
