using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.AssignDriver;

public sealed record AssignDriverCommand(Guid TripId, Guid DriverId) : IRequest<TripDto>, IAuditableRequest
{
    public string AuditAction => "AssignDriver";

    public string AuditEntityName => "Trip";

    public Guid? AuditEntityId => TripId;
}
