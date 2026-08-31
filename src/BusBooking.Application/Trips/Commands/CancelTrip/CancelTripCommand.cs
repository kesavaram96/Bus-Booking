using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Trips.Commands.CancelTrip;

public sealed record CancelTripCommand(Guid TripId) : IRequest, IAuditableRequest
{
    public string AuditAction => "CancelTrip";

    public string AuditEntityName => "Trip";

    public Guid? AuditEntityId => TripId;
}
