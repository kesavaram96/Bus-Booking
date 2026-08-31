using BusBooking.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Routes;

public class RouteStopTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesStop()
    {
        var stop = new RouteStop(
            Guid.NewGuid(),
            "Kadawatha",
            stopOrder: 2,
            expectedArrivalTime: TimeSpan.FromHours(21),
            expectedDepartureTime: TimeSpan.FromHours(21.25),
            allowPickup: true,
            allowDropOff: false);

        stop.StopOrder.Should().Be(2);
        stop.AllowPickup.Should().BeTrue();
        stop.AllowDropOff.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveStopOrder_Throws(int stopOrder)
    {
        var act = () => new RouteStop(Guid.NewGuid(), "Kadawatha", stopOrder, null, null, true, true);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateOrder_ChangesStopOrder()
    {
        var stop = new RouteStop(Guid.NewGuid(), "Kadawatha", 1, null, null, true, true);

        stop.UpdateOrder(5);

        stop.StopOrder.Should().Be(5);
    }
}
