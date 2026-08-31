using FluentValidation;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatNumber;

public sealed class UpdateSeatNumberCommandValidator : AbstractValidator<UpdateSeatNumberCommand>
{
    public UpdateSeatNumberCommandValidator()
    {
        RuleFor(x => x.SeatLayoutId).NotEmpty();
        RuleFor(x => x.SeatId).NotEmpty();
        RuleFor(x => x.SeatNumber).NotEmpty().MaximumLength(10);
    }
}
