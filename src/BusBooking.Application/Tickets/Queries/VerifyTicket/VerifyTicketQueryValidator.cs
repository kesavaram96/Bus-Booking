using FluentValidation;

namespace BusBooking.Application.Tickets.Queries.VerifyTicket;

public sealed class VerifyTicketQueryValidator : AbstractValidator<VerifyTicketQuery>
{
    public VerifyTicketQueryValidator()
    {
        RuleFor(x => x.TicketCode).NotEmpty();
    }
}
