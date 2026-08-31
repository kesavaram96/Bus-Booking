using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Trips.Commands.LockSeat;

public sealed class LockSeatCommandHandler : IRequestHandler<LockSeatCommand, SeatLockDto>
{
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;

    public LockSeatCommandHandler(IApplicationDbContext context, ISeatLockService seatLockService)
    {
        _context = context;
        _seatLockService = seatLockService;
    }

    public async Task<SeatLockDto> Handle(LockSeatCommand request, CancellationToken cancellationToken)
    {
        var tripSeat = await _context.TripSeats
            .FirstOrDefaultAsync(ts => ts.Id == request.TripSeatId && ts.TripId == request.TripId, cancellationToken)
            ?? throw new NotFoundException("TripSeat", request.TripSeatId);

        // Staff blocks are decided by the database — Redis is only the arbiter of the
        // Available<->Held race below. Whether the requested *segment* is actually free is
        // decided later, at booking time, against existing BookingPassenger rows (Phase 13);
        // a TripSeat row has no "booked" concept of its own.
        if (tripSeat.Status == TripSeatStatus.Blocked)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.TripSeatId), "This seat is blocked.")]);
        }

        var acquireResult = await _seatLockService.TryAcquireAsync(request.TripSeatId, LockDuration, cancellationToken);
        if (!acquireResult.Acquired)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.TripSeatId), "This seat is currently held by another customer.")
            ]);
        }

        tripSeat.Hold(acquireResult.LockId!, acquireResult.LockedUntil!.Value);
        await _context.SaveChangesAsync(cancellationToken);

        return new SeatLockDto(tripSeat.Id, acquireResult.LockId!, acquireResult.LockedUntil!.Value);
    }
}
