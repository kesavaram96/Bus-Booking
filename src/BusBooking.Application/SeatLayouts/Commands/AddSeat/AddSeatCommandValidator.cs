using FluentValidation;

namespace BusBooking.Application.SeatLayouts.Commands.AddSeat;

public sealed class AddSeatCommandValidator : AbstractValidator<AddSeatCommand>
{
    public AddSeatCommandValidator()
    {
        RuleFor(x => x.SeatLayoutId).NotEmpty();

        RuleFor(x => x.SeatNumber)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.Row).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Column).GreaterThanOrEqualTo(0);

        RuleFor(x => x.PositionType).IsInEnum();
    }
}
