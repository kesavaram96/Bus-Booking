using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.UnblockSeat;

public sealed class UnblockSeatCommandHandler : IRequestHandler<UnblockSeatCommand, TripSeatDto>
{
    private readonly IApplicationDbContext _context;

    public UnblockSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripSeatDto> Handle(UnblockSeatCommand request, CancellationToken cancellationToken)
    {
        var tripSeat = await _context.TripSeats
            .Include(ts => ts.Seat)
            .FirstOrDefaultAsync(ts => ts.Id == request.TripSeatId && ts.TripId == request.TripId, cancellationToken)
            ?? throw new NotFoundException("TripSeat", request.TripSeatId);

        tripSeat.Unblock();

        await _context.SaveChangesAsync(cancellationToken);

        return new TripSeatDto(
            tripSeat.Id, tripSeat.SeatId, tripSeat.Seat.SeatNumber, tripSeat.Seat.Row, tripSeat.Seat.Column,
            tripSeat.Seat.PositionType, tripSeat.Status);
    }
}
