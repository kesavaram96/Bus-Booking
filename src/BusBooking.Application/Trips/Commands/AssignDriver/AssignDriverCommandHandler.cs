using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.Common;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.AssignDriver;

public sealed class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand, TripDto>
{
    private readonly IApplicationDbContext _context;

    public AssignDriverCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(AssignDriverCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        await TripValidation.LoadActiveDriverOrNullAsync(_context, request.DriverId, cancellationToken);

        trip.AssignDriver(request.DriverId);

        await _context.SaveChangesAsync(cancellationToken);

        return trip.ToDto();
    }
}
