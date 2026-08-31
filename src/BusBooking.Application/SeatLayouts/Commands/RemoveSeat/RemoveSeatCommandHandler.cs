using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.SeatLayouts.Commands.RemoveSeat;

public sealed class RemoveSeatCommandHandler : IRequestHandler<RemoveSeatCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveSeatCommand request, CancellationToken cancellationToken)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(
            s => s.Id == request.SeatId && s.SeatLayoutId == request.SeatLayoutId,
            cancellationToken)
            ?? throw new NotFoundException("Seat", request.SeatId);

        // A seat used on any trip (Phase 10+) has a Restrict FK from TripSeat — check up front
        // for a friendly 400 instead of letting the DB throw a raw FK-violation 500.
        var isUsedOnATrip = await _context.TripSeats.AnyAsync(ts => ts.SeatId == request.SeatId, cancellationToken);
        if (isUsedOnATrip)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.SeatId), "This seat is used on at least one trip and cannot be removed.")
            ]);
        }

        _context.Seats.Remove(seat);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
