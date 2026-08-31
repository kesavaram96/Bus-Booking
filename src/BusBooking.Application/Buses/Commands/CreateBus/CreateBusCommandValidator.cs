using FluentValidation;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed class CreateBusCommandValidator : AbstractValidator<CreateBusCommand>
{
    public CreateBusCommandValidator()
    {
        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.BusType)
            .IsInEnum();
    }
}
