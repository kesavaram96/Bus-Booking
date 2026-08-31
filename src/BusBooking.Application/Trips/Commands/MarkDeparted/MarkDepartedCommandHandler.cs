using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.MarkDeparted;

public sealed class MarkDepartedCommandHandler : IRequestHandler<MarkDepartedCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkDepartedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkDepartedCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.MarkDeparted();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
