using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.Common;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.AssignBus;

public sealed class AssignBusCommandHandler : IRequestHandler<AssignBusCommand, TripDto>
{
    private readonly IApplicationDbContext _context;

    public AssignBusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(AssignBusCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        var bus = await TripValidation.LoadAssignableBusAsync(_context, request.BusId, cancellationToken);

        await TripValidation.EnsureBusHasNoOverlappingTripAsync(
            _context, request.BusId, trip.TripDate, trip.DepartureTime, trip.ExpectedArrivalTime, trip.Id, cancellationToken);

        trip.AssignBus(request.BusId);

        // Unconditional regeneration is safe only because nothing can hold/book a seat yet
        // (Phases 11/12). Once those exist, changing a trip's bus will need to account for
        // in-flight holds/bookings instead of blindly wiping and recreating TripSeats.
        await TripSeatGeneration.RegenerateForTripAsync(_context, trip.Id, bus.SeatLayoutId!.Value, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return trip.ToDto();
    }
}
