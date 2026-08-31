using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.BlockSeat;

public sealed class BlockSeatCommandHandler : IRequestHandler<BlockSeatCommand, TripSeatDto>
{
    private readonly IApplicationDbContext _context;

    public BlockSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripSeatDto> Handle(BlockSeatCommand request, CancellationToken cancellationToken)
    {
        var tripSeat = await _context.TripSeats
            .Include(ts => ts.Seat)
            .FirstOrDefaultAsync(ts => ts.Id == request.TripSeatId && ts.TripId == request.TripId, cancellationToken)
            ?? throw new NotFoundException("TripSeat", request.TripSeatId);

        tripSeat.Block();

        await _context.SaveChangesAsync(cancellationToken);

        return new TripSeatDto(
            tripSeat.Id, tripSeat.SeatId, tripSeat.Seat.SeatNumber, tripSeat.Seat.Row, tripSeat.Seat.Column,
            tripSeat.Seat.PositionType, tripSeat.Status);
    }
}
