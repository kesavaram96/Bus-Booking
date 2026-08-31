using FluentValidation;

namespace BusBooking.Application.Routes.Queries.GetRoutes;

public sealed class GetRoutesQueryValidator : AbstractValidator<GetRoutesQuery>
{
    public GetRoutesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
