using FluentValidation;

namespace BusBooking.Application.Trips.Queries.SearchTrips;

public sealed class SearchTripsQueryValidator : AbstractValidator<SearchTripsQuery>
{
    public SearchTripsQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().MaximumLength(100);
        RuleFor(x => x.To).NotEmpty().MaximumLength(100);

        RuleFor(x => x)
            .Must(x => !string.Equals(x.From?.Trim(), x.To?.Trim(), StringComparison.OrdinalIgnoreCase))
            .WithMessage("From and To must be different.")
            .WithName(nameof(SearchTripsQuery.To));

        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
