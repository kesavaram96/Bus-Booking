using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Bookings;

public class BookingPassengerTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesPassenger()
    {
        var passenger = new BookingPassenger(
            Guid.NewGuid(), " Nimal Perera ", " 0771234567 ", Gender.Male, "200012345678", "nimal@example.com",
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3500m);

        passenger.FullName.Should().Be("Nimal Perera");
        passenger.PhoneNumber.Should().Be("0771234567");
        passenger.NIC.Should().Be("200012345678");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_Succeeds()
    {
        var passenger = new BookingPassenger(
            Guid.NewGuid(), "Guest Passenger", "0770000000", Gender.Female, null, null,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3500m);

        passenger.NIC.Should().BeNull();
        passenger.Email.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveFare_Throws(decimal fare)
    {
        var act = () => new BookingPassenger(
            Guid.NewGuid(), "Someone", "0770000000", Gender.Other, null, null,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fare);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithEmptySeatId_Throws()
    {
        var act = () => new BookingPassenger(
            Guid.NewGuid(), "Someone", "0770000000", Gender.Other, null, null,
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 3500m);

        act.Should().Throw<ArgumentException>();
    }
}
