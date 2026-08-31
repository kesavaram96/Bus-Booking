using FluentValidation;

namespace BusBooking.Application.Trips.Commands.UnlockSeat;

public sealed class UnlockSeatCommandValidator : AbstractValidator<UnlockSeatCommand>
{
    public UnlockSeatCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.TripSeatId).NotEmpty();
        RuleFor(x => x.LockId).NotEmpty();
    }
}
