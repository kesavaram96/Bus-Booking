using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Tickets.DTOs;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Tickets.Queries.VerifyTicket;

/// <summary>
/// A ticket is only "Valid" while its booking is Confirmed — Cancelled/Completed/NoShow/
/// Refunded bookings all report a specific reason via BookingStatus/Reason rather than a bare
/// false, so staff scanning it can see exactly why boarding should be refused.
/// </summary>
public sealed class VerifyTicketQueryHandler : IRequestHandler<VerifyTicketQuery, TicketVerificationDto>
{
    private readonly IApplicationDbContext _context;

    public VerifyTicketQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketVerificationDto> Handle(VerifyTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Booking)
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.Seat)
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.PickupStop)
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.DropOffStop)
            .FirstOrDefaultAsync(t => t.TicketCode == request.TicketCode, cancellationToken);

        if (ticket is null)
        {
            return new TicketVerificationDto(false, "Ticket not found.", null, null, null, null, null, null, null, null, null, null, null, null);
        }

        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .FirstOrDefaultAsync(t => t.Id == ticket.Booking.TripId, cancellationToken);

        var isValid = ticket.Booking.Status == BookingStatus.Confirmed;
        var reason = isValid ? null : $"Booking is {ticket.Booking.Status}.";

        return new TicketVerificationDto(
            isValid,
            reason,
            ticket.TicketNumber,
            ticket.Booking.BookingNumber,
            ticket.Booking.Status,
            ticket.BookingPassenger.FullName,
            ticket.BookingPassenger.Seat.SeatNumber,
            trip?.Id,
            trip?.TripDate,
            trip?.DepartureTime,
            trip?.Route.From,
            trip?.Route.To,
            ticket.BookingPassenger.PickupStop.StopName,
            ticket.BookingPassenger.DropOffStop.StopName);
    }
}
