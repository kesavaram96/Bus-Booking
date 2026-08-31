using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.Common;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.UpdateTrip;

public sealed class UpdateTripCommandHandler : IRequestHandler<UpdateTripCommand, TripDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateTripCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(UpdateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Trip", request.Id);

        await TripValidation.EnsureBusHasNoOverlappingTripAsync(
            _context, trip.BusId, request.TripDate, request.DepartureTime, request.ExpectedArrivalTime, trip.Id, cancellationToken);

        trip.UpdateSchedule(request.TripDate, request.DepartureTime, request.ExpectedArrivalTime, request.Fare);

        await _context.SaveChangesAsync(cancellationToken);

        return trip.ToDto();
    }
}
