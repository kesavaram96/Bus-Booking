using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.Common;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Entities;
using MediatR;

namespace BusBooking.Application.Trips.Commands.CreateTrip;

public sealed class CreateTripCommandHandler : IRequestHandler<CreateTripCommand, TripDto>
{
    private readonly IApplicationDbContext _context;

    public CreateTripCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        await TripValidation.LoadActiveRouteAsync(_context, request.RouteId, cancellationToken);
        var bus = await TripValidation.LoadAssignableBusAsync(_context, request.BusId, cancellationToken);
        await TripValidation.LoadActiveDriverOrNullAsync(_context, request.DriverId, cancellationToken);

        await TripValidation.EnsureBusHasNoOverlappingTripAsync(
            _context, request.BusId, request.TripDate, request.DepartureTime, request.ExpectedArrivalTime, null, cancellationToken);

        var trip = new Trip(
            request.RouteId,
            request.BusId,
            request.TripDate,
            request.DepartureTime,
            request.ExpectedArrivalTime,
            request.DriverId,
            request.Fare);

        _context.Trips.Add(trip);

        // Trip.Id is generated client-side (BaseEntity), so it's already known here — trip
        // creation and seat generation commit together in the one SaveChangesAsync below.
        await TripSeatGeneration.GenerateForTripAsync(_context, trip.Id, bus.SeatLayoutId!.Value, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return trip.ToDto();
    }
}
