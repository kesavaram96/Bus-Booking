using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Routes.Commands.DeactivateRoute;

public sealed record DeactivateRouteCommand(Guid Id) : IRequest, IAuditableRequest
{
    public string AuditAction => "DeactivateRoute";

    public string AuditEntityName => "Route";

    public Guid? AuditEntityId => Id;
}
