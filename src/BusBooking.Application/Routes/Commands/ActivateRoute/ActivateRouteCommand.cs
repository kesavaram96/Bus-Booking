using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Routes.Commands.ActivateRoute;

public sealed record ActivateRouteCommand(Guid Id) : IRequest, IAuditableRequest
{
    public string AuditAction => "ActivateRoute";

    public string AuditEntityName => "Route";

    public Guid? AuditEntityId => Id;
}
