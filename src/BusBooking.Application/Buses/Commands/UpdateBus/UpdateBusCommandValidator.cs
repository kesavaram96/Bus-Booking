using FluentValidation;

namespace BusBooking.Application.Buses.Commands.UpdateBus;

public sealed class UpdateBusCommandValidator : AbstractValidator<UpdateBusCommand>
{
    public UpdateBusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.BusType)
            .IsInEnum();
    }
}
