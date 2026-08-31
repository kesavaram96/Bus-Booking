using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Buses;

public class BusTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesActiveBus()
    {
        var bus = new Bus("np-ab-1234", "59-seater coach", BusType.Luxury);

        bus.RegistrationNumber.Should().Be("NP-AB-1234");
        bus.Status.Should().Be(BusStatus.Active);
        bus.SeatLayoutId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithMissingRegistrationNumber_Throws(string? registrationNumber)
    {
        var act = () => new Bus(registrationNumber!, null, BusType.Normal);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignSeatLayout_WithValidId_SetsSeatLayoutId()
    {
        var bus = new Bus("NB-1111", null, BusType.SemiLuxury);
        var seatLayoutId = Guid.NewGuid();

        bus.AssignSeatLayout(seatLayoutId);

        bus.SeatLayoutId.Should().Be(seatLayoutId);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesStatus()
    {
        var bus = new Bus("NB-2222", null, BusType.AC);

        bus.Deactivate();
        bus.Status.Should().Be(BusStatus.Inactive);

        bus.Activate();
        bus.Status.Should().Be(BusStatus.Active);
    }
}
