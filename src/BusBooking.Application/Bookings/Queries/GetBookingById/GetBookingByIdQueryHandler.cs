using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Bookings.Queries.GetBookingById;

public sealed class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    private readonly IApplicationDbContext _context;

    public GetBookingByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Passengers).ThenInclude(p => p.PickupStop)
            .Include(b => b.Passengers).ThenInclude(p => p.DropOffStop)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Booking", request.Id);

        return booking.ToDto();
    }
}
