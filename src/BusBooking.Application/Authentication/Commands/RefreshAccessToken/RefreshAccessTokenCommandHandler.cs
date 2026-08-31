using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Interfaces;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshAccessTokenCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResult> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var (success, userId, newRawToken, newExpiresAtUtc) =
            await _refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

        if (!success || userId is null || newRawToken is null || newExpiresAtUtc is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await _identityService.FindByIdAsync(userId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("User no longer exists.");

        var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateAccessToken(user);

        return new AuthResult(
            accessToken,
            accessTokenExpiresAtUtc,
            newRawToken,
            newExpiresAtUtc.Value,
            new UserDto(user.Id, user.UserName, user.Email, user.FullName, user.Roles));
    }
}
