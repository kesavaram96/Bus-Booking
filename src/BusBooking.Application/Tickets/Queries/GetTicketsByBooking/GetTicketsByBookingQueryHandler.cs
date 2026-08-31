using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Tickets.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Tickets.Queries.GetTicketsByBooking;

public sealed class GetTicketsByBookingQueryHandler : IRequestHandler<GetTicketsByBookingQuery, IReadOnlyList<TicketDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public GetTicketsByBookingQueryHandler(IApplicationDbContext context, IQrCodeGenerator qrCodeGenerator)
    {
        _context = context;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public async Task<IReadOnlyList<TicketDto>> Handle(GetTicketsByBookingQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking", request.BookingId);

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.Seat)
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.PickupStop)
            .Include(t => t.BookingPassenger).ThenInclude(bp => bp.DropOffStop)
            .Where(t => t.BookingId == request.BookingId)
            .ToListAsync(cancellationToken);

        // QR image generation is CPU-only and not translatable to SQL, so it happens here,
        // after materialization, one ticket at a time.
        return tickets
            .Select(t => new TicketDto(
                t.Id,
                t.BookingId,
                booking.BookingNumber,
                t.TicketNumber,
                t.TicketCode,
                Convert.ToBase64String(_qrCodeGenerator.GeneratePng(t.TicketCode)),
                booking.TripId,
                t.BookingPassenger.FullName,
                t.BookingPassenger.Seat.SeatNumber,
                t.BookingPassenger.PickupStop.StopName,
                t.BookingPassenger.DropOffStop.StopName))
            .ToList();
    }
}
