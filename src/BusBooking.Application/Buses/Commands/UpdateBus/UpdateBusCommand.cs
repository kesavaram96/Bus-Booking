using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Auditing;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Buses.Commands.UpdateBus;

public sealed record UpdateBusCommand(
    Guid Id,
    string RegistrationNumber,
    string? Description,
    BusType BusType) : IRequest<BusDto>, IAuditableRequest
{
    public string AuditAction => "UpdateBus";

    public string AuditEntityName => "Bus";

    public Guid? AuditEntityId => Id;
}
