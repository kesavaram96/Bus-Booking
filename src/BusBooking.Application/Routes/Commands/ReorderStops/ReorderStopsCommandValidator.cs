using FluentValidation;

namespace BusBooking.Application.Routes.Commands.ReorderStops;

public sealed class ReorderStopsCommandValidator : AbstractValidator<ReorderStopsCommand>
{
    public ReorderStopsCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();

        RuleFor(x => x.OrderedStopIds)
            .NotEmpty();

        RuleFor(x => x.OrderedStopIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Ordered stop list must not contain duplicate stop ids.")
            .When(x => x.OrderedStopIds is { Count: > 0 });
    }
}
