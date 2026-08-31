using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Auditing;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed record CreateBusCommand(
    string RegistrationNumber,
    string? Description,
    BusType BusType) : IRequest<BusDto>, IAuditableRequest
{
    public string AuditAction => "CreateBus";

    public string AuditEntityName => "Bus";

    public Guid? AuditEntityId => null;
}
