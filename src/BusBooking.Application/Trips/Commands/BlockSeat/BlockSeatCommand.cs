using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.BlockSeat;

public sealed record BlockSeatCommand(Guid TripId, Guid TripSeatId) : IRequest<TripSeatDto>, IAuditableRequest
{
    public string AuditAction => "BlockSeat";

    public string AuditEntityName => "TripSeat";

    public Guid? AuditEntityId => TripSeatId;
}
