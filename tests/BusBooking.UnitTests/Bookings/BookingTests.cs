using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Bookings;

public class BookingTests
{
    private static BookingPassenger CreatePassenger(decimal fare = 3500m) =>
        new(
            Guid.NewGuid(),
            "Nimal Perera",
            "0771234567",
            Gender.Male,
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            fare);

    [Fact]
    public void Constructor_WithValidTripId_StartsPendingWithZeroTotal()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.TotalAmount.Should().Be(0m);
        booking.Passengers.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_GeneratesBookingNumberWithDatePrefix()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.BookingNumber.Should().StartWith("BK" + DateTime.UtcNow.ToString("yyMMdd"));
        booking.BookingNumber.Length.Should().Be(14);
    }

    [Fact]
    public void Constructor_WithEmptyTripId_Throws()
    {
        var act = () => new Booking(Guid.Empty, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ForGuestOrStaffBooking_AllowsNullCustomerId()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.CustomerId.Should().BeNull();
    }

    [Fact]
    public void AddPassenger_AccumulatesTotalAmount()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.AddPassenger(CreatePassenger(3500m));
        booking.AddPassenger(CreatePassenger(3500m));

        booking.Passengers.Should().HaveCount(2);
        booking.TotalAmount.Should().Be(7000m);
    }

    [Fact]
    public void TwoBookings_GetDifferentBookingNumbers()
    {
        var first = new Booking(Guid.NewGuid(), null);
        var second = new Booking(Guid.NewGuid(), null);

        first.BookingNumber.Should().NotBe(second.BookingNumber);
    }

    [Fact]
    public void Confirm_FromPending_TransitionsToConfirmed()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_Throws()
    {
        var booking = new Booking(Guid.NewGuid(), null);
        booking.Confirm();

        var act = () => booking.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPending_RecordsReasonAndWho()
    {
        var booking = new Booking(Guid.NewGuid(), null);
        var cancelledBy = Guid.NewGuid();

        booking.Cancel("Change of plans", cancelledBy);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Change of plans");
        booking.CancelledBy.Should().Be(cancelledBy);
        booking.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromConfirmed_Succeeds()
    {
        var booking = new Booking(Guid.NewGuid(), null);
        booking.Confirm();

        booking.Cancel("No longer needed", null);

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WithSystemTriggeredNullCancelledBy_Succeeds()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        booking.Cancel("Trip cancelled.", null);

        booking.CancelledBy.Should().BeNull();
    }

    [Fact]
    public void Cancel_WithEmptyReason_Throws()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        var act = () => booking.Cancel(" ", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var booking = new Booking(Guid.NewGuid(), null);
        booking.Cancel("First reason", null);

        var act = () => booking.Cancel("Second reason", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkRefunded_FromCancelled_TransitionsToRefunded()
    {
        var booking = new Booking(Guid.NewGuid(), null);
        booking.Cancel("Change of plans", null);

        booking.MarkRefunded();

        booking.Status.Should().Be(BookingStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_WhenNotCancelled_Throws()
    {
        var booking = new Booking(Guid.NewGuid(), null);

        var act = () => booking.MarkRefunded();

        act.Should().Throw<InvalidOperationException>();
    }
}
