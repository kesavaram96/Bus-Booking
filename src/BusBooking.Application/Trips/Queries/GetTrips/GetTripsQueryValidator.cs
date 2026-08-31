using FluentValidation;

namespace BusBooking.Application.Trips.Queries.GetTrips;

public sealed class GetTripsQueryValidator : AbstractValidator<GetTripsQuery>
{
    public GetTripsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x)
            .Must(x => x.ToDate >= x.FromDate)
            .WithMessage("ToDate cannot be before FromDate.")
            .WithName(nameof(GetTripsQuery.ToDate))
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}
