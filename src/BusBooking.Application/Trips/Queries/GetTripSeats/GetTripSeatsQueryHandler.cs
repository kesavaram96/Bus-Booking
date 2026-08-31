using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.GetTripSeats;

public sealed class GetTripSeatsQueryHandler : IRequestHandler<GetTripSeatsQuery, IReadOnlyList<TripSeatDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTripSeatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TripSeatDto>> Handle(GetTripSeatsQuery request, CancellationToken cancellationToken)
    {
        var tripExists = await _context.Trips.AnyAsync(t => t.Id == request.TripId, cancellationToken);
        if (!tripExists)
        {
            throw new NotFoundException("Trip", request.TripId);
        }

        return await _context.TripSeats
            .AsNoTracking()
            .Include(ts => ts.Seat)
            .Where(ts => ts.TripId == request.TripId)
            .OrderBy(ts => ts.Seat.Row).ThenBy(ts => ts.Seat.Column)
            .Select(ts => new TripSeatDto(
                ts.Id, ts.SeatId, ts.Seat.SeatNumber, ts.Seat.Row, ts.Seat.Column, ts.Seat.PositionType, ts.Status))
            .ToListAsync(cancellationToken);
    }
}
