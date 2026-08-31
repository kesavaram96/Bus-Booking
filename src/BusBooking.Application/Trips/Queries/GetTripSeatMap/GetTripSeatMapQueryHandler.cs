using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.GetTripSeatMap;

public sealed class GetTripSeatMapQueryHandler : IRequestHandler<GetTripSeatMapQuery, SeatMapDto>
{
    private readonly IApplicationDbContext _context;

    public GetTripSeatMapQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatMapDto> Handle(GetTripSeatMapQuery request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Bus)
            .ThenInclude(b => b.SeatLayout)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        var seatLayout = trip.Bus.SeatLayout
            ?? throw new InvalidOperationException("This trip's bus has no seat layout assigned.");

        var seats = await _context.TripSeats
            .AsNoTracking()
            .Include(ts => ts.Seat)
            .Where(ts => ts.TripId == request.TripId)
            .OrderBy(ts => ts.Seat.Row).ThenBy(ts => ts.Seat.Column)
            .Select(ts => new PublicSeatMapEntryDto(
                ts.Id, ts.Seat.SeatNumber, ts.Seat.Row, ts.Seat.Column, ts.Seat.PositionType, ts.Status))
            .ToListAsync(cancellationToken);

        return new SeatMapDto(trip.Id, seatLayout.Rows, seatLayout.Columns, seats);
    }
}
