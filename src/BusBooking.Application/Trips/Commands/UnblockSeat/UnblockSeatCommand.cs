using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.UnblockSeat;

public sealed record UnblockSeatCommand(Guid TripId, Guid TripSeatId) : IRequest<TripSeatDto>, IAuditableRequest
{
    public string AuditAction => "UnblockSeat";

    public string AuditEntityName => "TripSeat";

    public Guid? AuditEntityId => TripSeatId;
}
