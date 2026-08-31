using FluentValidation;

namespace BusBooking.Application.Buses.Commands.AssignSeatLayout;

public sealed class AssignSeatLayoutCommandValidator : AbstractValidator<AssignSeatLayoutCommand>
{
    public AssignSeatLayoutCommandValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.SeatLayoutId).NotEmpty();
    }
}
