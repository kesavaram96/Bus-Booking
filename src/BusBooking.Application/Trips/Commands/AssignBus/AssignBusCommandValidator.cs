using FluentValidation;

namespace BusBooking.Application.Trips.Commands.AssignBus;

public sealed class AssignBusCommandValidator : AbstractValidator<AssignBusCommand>
{
    public AssignBusCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();
    }
}
