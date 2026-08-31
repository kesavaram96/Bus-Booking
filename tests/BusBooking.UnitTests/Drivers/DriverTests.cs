using BusBooking.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Drivers;

public class DriverTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesActiveDriver()
    {
        var driver = new Driver("Nimal Perera", "0771234567", " b1234567 ", new DateOnly(2027, 1, 1));

        driver.LicenseNumber.Should().Be("B1234567");
        driver.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingFullName_Throws(string fullName)
    {
        var act = () => new Driver(fullName, "0771234567", "B1234567", new DateOnly(2027, 1, 1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var driver = new Driver("Nimal Perera", "0771234567", "B1234567", new DateOnly(2027, 1, 1));

        driver.Deactivate();

        driver.IsActive.Should().BeFalse();
    }
}
