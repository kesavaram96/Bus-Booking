using FluentValidation;

namespace BusBooking.Application.Customers.Commands.ChangePhoneNumber;

public sealed class ChangePhoneNumberCommandValidator : AbstractValidator<ChangePhoneNumberCommand>
{
    public ChangePhoneNumberCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9\s-]{7,20}$")
            .WithMessage("Phone number is not valid.");
    }
}
