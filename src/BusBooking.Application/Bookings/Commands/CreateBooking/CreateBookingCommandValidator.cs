using FluentValidation;

namespace BusBooking.Application.Bookings.Commands.CreateBooking;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    private const int MaxPassengersPerBooking = 10;

    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();

        RuleFor(x => x.Passengers)
            .NotEmpty()
            .WithMessage("At least one passenger is required.");

        RuleFor(x => x.Passengers)
            .Must(p => p.Count <= MaxPassengersPerBooking)
            .WithMessage($"A single booking cannot contain more than {MaxPassengersPerBooking} passengers.")
            .When(x => x.Passengers is { Count: > 0 });

        RuleFor(x => x.Passengers)
            .Must(p => p.Select(x => x.TripSeatId).Distinct().Count() == p.Count)
            .WithMessage("Each passenger must have a different seat.")
            .When(x => x.Passengers is { Count: > 0 });

        RuleForEach(x => x.Passengers).SetValidator(new BookingPassengerInputValidator());
    }
}

public sealed class BookingPassengerInputValidator : AbstractValidator<BookingPassengerInput>
{
    public BookingPassengerInputValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9\s-]{7,20}$")
            .WithMessage("Phone number is not valid.");

        RuleFor(x => x.Gender).IsInEnum();

        RuleFor(x => x.NIC)
            .Matches(@"^(\d{9}[vVxX]|\d{12})$")
            .WithMessage("NIC must be a valid Sri Lankan NIC (9 digits + V/X, or 12 digits).")
            .When(x => !string.IsNullOrWhiteSpace(x.NIC));

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PickupStopId).NotEmpty();
        RuleFor(x => x.DropOffStopId).NotEmpty();
        RuleFor(x => x.TripSeatId).NotEmpty();
        RuleFor(x => x.LockId).NotEmpty();
    }
}
