using FluentValidation;

namespace BusBooking.Application.Routes.Commands.AddStop;

public sealed class AddStopCommandValidator : AbstractValidator<AddStopCommand>
{
    public AddStopCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.StopName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.ExpectedArrivalTime)
            .InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24))
            .When(x => x.ExpectedArrivalTime.HasValue);

        RuleFor(x => x.ExpectedDepartureTime)
            .InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24))
            .When(x => x.ExpectedDepartureTime.HasValue);

        RuleFor(x => x)
            .Must(x => x.ExpectedDepartureTime >= x.ExpectedArrivalTime)
            .WithMessage("Expected departure time cannot be before expected arrival time.")
            .WithName(nameof(AddStopCommand.ExpectedDepartureTime))
            .When(x => x.ExpectedArrivalTime.HasValue && x.ExpectedDepartureTime.HasValue);
    }
}
