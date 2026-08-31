using FluentValidation;

namespace BusBooking.Application.Buses.Queries.GetBuses;

public sealed class GetBusesQueryValidator : AbstractValidator<GetBusesQuery>
{
    public GetBusesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
