using FluentValidation;

namespace BusBooking.Application.Trips.Commands.CreateTrip;

public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();

        RuleFor(x => x.TripDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Trip date cannot be in the past.");

        RuleFor(x => x.DepartureTime)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .LessThan(TimeSpan.FromHours(24));

        RuleFor(x => x.ExpectedArrivalTime)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .LessThan(TimeSpan.FromHours(24));

        RuleFor(x => x.Fare).GreaterThan(0);
    }
}
