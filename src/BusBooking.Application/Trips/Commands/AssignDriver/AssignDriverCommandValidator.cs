using FluentValidation;

namespace BusBooking.Application.Trips.Commands.AssignDriver;

public sealed class AssignDriverCommandValidator : AbstractValidator<AssignDriverCommand>
{
    public AssignDriverCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
    }
}
