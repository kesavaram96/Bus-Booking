using BusBooking.Application.Common.Interfaces;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Idempotent by design: revoking an already-revoked or unknown token is a no-op,
        // so logout never leaks whether a token existed.
        await _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}
