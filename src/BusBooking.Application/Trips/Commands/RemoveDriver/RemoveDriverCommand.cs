using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.RemoveDriver;

public sealed record RemoveDriverCommand(Guid TripId) : IRequest<TripDto>, IAuditableRequest
{
    public string AuditAction => "RemoveDriver";

    public string AuditEntityName => "Trip";

    public Guid? AuditEntityId => TripId;
}
