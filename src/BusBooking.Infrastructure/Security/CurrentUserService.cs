using System.Security.Claims;
using BusBooking.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BusBooking.Infrastructure.Security;

/// <summary>The one place IHttpContextAccessor is used in this codebase — everywhere else,
/// "who's acting" is an explicit command parameter the controller decides from JWT claims
/// itself (see ICurrentUserService's own doc comment for why audit logging is the exception).</summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
