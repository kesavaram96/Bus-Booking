using FluentValidation;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatPosition;

public sealed class UpdateSeatPositionCommandValidator : AbstractValidator<UpdateSeatPositionCommand>
{
    public UpdateSeatPositionCommandValidator()
    {
        RuleFor(x => x.SeatLayoutId).NotEmpty();
        RuleFor(x => x.SeatId).NotEmpty();
        RuleFor(x => x.Row).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Column).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PositionType).IsInEnum();
    }
}
