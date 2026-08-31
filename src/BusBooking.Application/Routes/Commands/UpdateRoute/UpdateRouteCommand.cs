using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Commands.UpdateRoute;

public sealed record UpdateRouteCommand(Guid Id, string Name, string From, string To) : IRequest<RouteDto>, IAuditableRequest
{
    public string AuditAction => "UpdateRoute";

    public string AuditEntityName => "Route";

    public Guid? AuditEntityId => Id;
}
