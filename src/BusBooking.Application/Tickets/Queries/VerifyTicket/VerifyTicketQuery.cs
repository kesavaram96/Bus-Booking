using BusBooking.Application.Tickets.DTOs;
using MediatR;

namespace BusBooking.Application.Tickets.Queries.VerifyTicket;

public sealed record VerifyTicketQuery(string TicketCode) : IRequest<TicketVerificationDto>;
