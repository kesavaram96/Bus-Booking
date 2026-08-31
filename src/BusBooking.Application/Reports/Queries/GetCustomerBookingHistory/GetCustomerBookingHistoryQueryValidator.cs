using FluentValidation;

namespace BusBooking.Application.Reports.Queries.GetCustomerBookingHistory;

public sealed class GetCustomerBookingHistoryQueryValidator : AbstractValidator<GetCustomerBookingHistoryQuery>
{
    public GetCustomerBookingHistoryQueryValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
