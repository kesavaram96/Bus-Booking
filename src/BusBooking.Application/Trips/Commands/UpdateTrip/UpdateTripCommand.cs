using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.UpdateTrip;

public sealed record UpdateTripCommand(
    Guid Id,
    DateOnly TripDate,
    TimeSpan DepartureTime,
    TimeSpan ExpectedArrivalTime,
    decimal Fare) : IRequest<TripDto>, IAuditableRequest
{
    public string AuditAction => "UpdateTrip";

    public string AuditEntityName => "Trip";

    public Guid? AuditEntityId => Id;
}
