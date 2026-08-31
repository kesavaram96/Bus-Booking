using BusBooking.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Routes;

public class RouteTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInactiveDraftRoute()
    {
        var route = new Route("Colombo - Jaffna", "Colombo", "Jaffna");

        // Starts inactive: with zero stops it can't be used for trips until stops are
        // added and it is explicitly activated (Activate() enforces >= 2 stops).
        route.IsActive.Should().BeFalse();
        route.Stops.Should().BeEmpty();
    }

    [Fact]
    public void Activate_ThenDeactivate_TogglesStatus()
    {
        var route = new Route("Colombo - Jaffna", "Colombo", "Jaffna");

        route.Activate();
        route.IsActive.Should().BeTrue();

        route.Deactivate();
        route.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithSameFromAndTo_Throws()
    {
        var act = () => new Route("Invalid Route", "Colombo", "Colombo");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddStop_AddsStopToCollection()
    {
        var route = new Route("Colombo - Jaffna", "Colombo", "Jaffna");
        var stop = new RouteStop(route.Id, "Kurunegala", 1, null, null, allowPickup: true, allowDropOff: true);

        route.AddStop(stop);

        route.Stops.Should().ContainSingle().Which.Should().Be(stop);
    }
}
