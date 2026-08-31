using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.MarkCompleted;

public sealed class MarkCompletedCommandHandler : IRequestHandler<MarkCompletedCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkCompletedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkCompletedCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.MarkCompleted();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
