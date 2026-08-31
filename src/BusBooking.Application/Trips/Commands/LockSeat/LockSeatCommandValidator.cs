using FluentValidation;

namespace BusBooking.Application.Trips.Commands.LockSeat;

public sealed class LockSeatCommandValidator : AbstractValidator<LockSeatCommand>
{
    public LockSeatCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.TripSeatId).NotEmpty();
    }
}
