using FluentValidation;

namespace BusBooking.Application.Routes.Commands.UpdateRoute;

public sealed class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.From).NotEmpty().MaximumLength(100);
        RuleFor(x => x.To).NotEmpty().MaximumLength(100);

        RuleFor(x => x)
            .Must(x => !string.Equals(x.From?.Trim(), x.To?.Trim(), StringComparison.OrdinalIgnoreCase))
            .WithMessage("From and To must be different.")
            .WithName(nameof(UpdateRouteCommand.To));
    }
}
