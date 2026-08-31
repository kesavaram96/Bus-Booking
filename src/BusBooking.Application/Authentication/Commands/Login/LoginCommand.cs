using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.Login;

public sealed record LoginCommand(string UsernameOrEmail, string Password) : IRequest<AuthResult>, IAuditableRequest
{
    public string AuditAction => "Login";

    public string AuditEntityName => "User";

    public Guid? AuditEntityId => null;
}
