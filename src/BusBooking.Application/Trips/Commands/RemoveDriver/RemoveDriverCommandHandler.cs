using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.RemoveDriver;

public sealed class RemoveDriverCommandHandler : IRequestHandler<RemoveDriverCommand, TripDto>
{
    private readonly IApplicationDbContext _context;

    public RemoveDriverCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(RemoveDriverCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.RemoveDriver();

        await _context.SaveChangesAsync(cancellationToken);

        return trip.ToDto();
    }
}
