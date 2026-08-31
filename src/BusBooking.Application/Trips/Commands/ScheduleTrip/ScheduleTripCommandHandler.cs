using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.ScheduleTrip;

public sealed class ScheduleTripCommandHandler : IRequestHandler<ScheduleTripCommand>
{
    private readonly IApplicationDbContext _context;

    public ScheduleTripCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ScheduleTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.Schedule();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
