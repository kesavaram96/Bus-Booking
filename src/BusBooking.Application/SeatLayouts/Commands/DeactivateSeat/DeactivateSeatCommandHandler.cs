using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.SeatLayouts.Commands.DeactivateSeat;

public sealed class DeactivateSeatCommandHandler : IRequestHandler<DeactivateSeatCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeactivateSeatCommand request, CancellationToken cancellationToken)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(
            s => s.Id == request.SeatId && s.SeatLayoutId == request.SeatLayoutId,
            cancellationToken)
            ?? throw new NotFoundException("Seat", request.SeatId);

        seat.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
