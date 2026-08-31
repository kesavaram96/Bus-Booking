using FluentValidation;

namespace BusBooking.Application.Bookings.Commands.CancelBooking;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.CancellationReason).NotEmpty().MaximumLength(500);
    }
}
