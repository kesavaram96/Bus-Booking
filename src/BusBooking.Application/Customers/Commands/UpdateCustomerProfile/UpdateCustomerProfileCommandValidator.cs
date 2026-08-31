using FluentValidation;

namespace BusBooking.Application.Customers.Commands.UpdateCustomerProfile;

public sealed class UpdateCustomerProfileCommandValidator : AbstractValidator<UpdateCustomerProfileCommand>
{
    public UpdateCustomerProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.NIC)
            .Matches(@"^(\d{9}[vVxX]|\d{12})$")
            .WithMessage("NIC must be a valid Sri Lankan NIC (9 digits + V/X, or 12 digits).")
            .When(x => !string.IsNullOrWhiteSpace(x.NIC));

        RuleFor(x => x.DateOfBirth)
            .LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);
    }
}
