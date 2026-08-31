using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.SeatLayouts.Commands.ActivateSeat;

public sealed class ActivateSeatCommandHandler : IRequestHandler<ActivateSeatCommand>
{
    private readonly IApplicationDbContext _context;

    public ActivateSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ActivateSeatCommand request, CancellationToken cancellationToken)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(
            s => s.Id == request.SeatId && s.SeatLayoutId == request.SeatLayoutId,
            cancellationToken)
            ?? throw new NotFoundException("Seat", request.SeatId);

        seat.Activate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
