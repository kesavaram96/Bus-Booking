using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Commands.CreateRoute;

public sealed record CreateRouteCommand(string Name, string From, string To) : IRequest<RouteDto>, IAuditableRequest
{
    public string AuditAction => "CreateRoute";

    public string AuditEntityName => "Route";

    public Guid? AuditEntityId => null;
}
