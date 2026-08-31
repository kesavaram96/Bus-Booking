using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.CreateTrip;

public sealed record CreateTripCommand(
    Guid RouteId,
    Guid BusId,
    DateOnly TripDate,
    TimeSpan DepartureTime,
    TimeSpan ExpectedArrivalTime,
    Guid? DriverId,
    decimal Fare) : IRequest<TripDto>, IAuditableRequest
{
    public string AuditAction => "CreateTrip";

    public string AuditEntityName => "Trip";

    public Guid? AuditEntityId => null;
}
