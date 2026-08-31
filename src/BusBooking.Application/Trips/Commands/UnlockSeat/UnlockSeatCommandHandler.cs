using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Trips.Commands.UnlockSeat;

public sealed class UnlockSeatCommandHandler : IRequestHandler<UnlockSeatCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISeatLockService _seatLockService;

    public UnlockSeatCommandHandler(IApplicationDbContext context, ISeatLockService seatLockService)
    {
        _context = context;
        _seatLockService = seatLockService;
    }

    public async Task Handle(UnlockSeatCommand request, CancellationToken cancellationToken)
    {
        var tripSeat = await _context.TripSeats
            .FirstOrDefaultAsync(ts => ts.Id == request.TripSeatId && ts.TripId == request.TripId, cancellationToken)
            ?? throw new NotFoundException("TripSeat", request.TripSeatId);

        var releaseResult = await _seatLockService.ReleaseAsync(request.TripSeatId, request.LockId, cancellationToken);

        if (releaseResult == SeatLockReleaseResult.Mismatch)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.LockId), "Invalid lock token.")]);
        }

        // Released or NotFound (already expired in Redis): bring the DB in line if it still
        // shows this seat held under this exact token — a safe, idempotent cleanup either way.
        if (tripSeat.Status == TripSeatStatus.Held && tripSeat.LockId == request.LockId)
        {
            tripSeat.ReleaseHold();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
