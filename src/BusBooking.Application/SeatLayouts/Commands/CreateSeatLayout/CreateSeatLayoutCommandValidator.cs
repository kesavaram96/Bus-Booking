using FluentValidation;

namespace BusBooking.Application.SeatLayouts.Commands.CreateSeatLayout;

public sealed class CreateSeatLayoutCommandValidator : AbstractValidator<CreateSeatLayoutCommand>
{
    public CreateSeatLayoutCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Rows)
            .GreaterThan(0)
            .LessThanOrEqualTo(60);

        RuleFor(x => x.Columns)
            .GreaterThan(0)
            .LessThanOrEqualTo(10);
    }
}
