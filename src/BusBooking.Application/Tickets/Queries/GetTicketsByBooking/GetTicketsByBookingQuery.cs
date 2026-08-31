using BusBooking.Application.Tickets.DTOs;
using MediatR;

namespace BusBooking.Application.Tickets.Queries.GetTicketsByBooking;

public sealed record GetTicketsByBookingQuery(Guid BookingId) : IRequest<IReadOnlyList<TicketDto>>;
