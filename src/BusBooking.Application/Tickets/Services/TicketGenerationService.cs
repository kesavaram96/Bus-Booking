using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Tickets.Services;

public sealed class TicketGenerationService : ITicketGenerationService
{
    private readonly IApplicationDbContext _context;

    public TicketGenerationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GenerateForBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Booking", bookingId);

        var passengerIdsWithTickets = await _context.Tickets
            .Where(t => t.BookingId == bookingId)
            .Select(t => t.BookingPassengerId)
            .ToListAsync(cancellationToken);

        foreach (var passenger in booking.Passengers)
        {
            if (passengerIdsWithTickets.Contains(passenger.Id))
            {
                continue;
            }

            _context.Tickets.Add(new Ticket(booking.Id, passenger.Id));
        }

        // Deliberately no SaveChangesAsync here — the caller commits Booking, Payment and
        // these Tickets together in one transaction.
    }
}
