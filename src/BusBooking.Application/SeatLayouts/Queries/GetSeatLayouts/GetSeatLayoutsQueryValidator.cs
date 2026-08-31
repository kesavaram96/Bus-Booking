using FluentValidation;

namespace BusBooking.Application.SeatLayouts.Queries.GetSeatLayouts;

public sealed class GetSeatLayoutsQueryValidator : AbstractValidator<GetSeatLayoutsQuery>
{
    public GetSeatLayoutsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
