using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;

namespace BusBooking.Application.Authentication.Common;

/// <summary>
/// Issues a fresh access/refresh token pair for an already-authenticated user.
/// Shared by RegisterCustomer and Login, which both start from a validated identity
/// rather than an existing refresh token (unlike RefreshAccessToken, which rotates one).
/// </summary>
internal static class AuthResultFactory
{
    public static async Task<AuthResult> CreateAsync(
        AuthenticatedUserDto user,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var (accessToken, accessTokenExpiresAtUtc) = jwtTokenService.GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpiresAtUtc) = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return new AuthResult(
            accessToken,
            accessTokenExpiresAtUtc,
            refreshToken,
            refreshTokenExpiresAtUtc,
            new UserDto(user.Id, user.UserName, user.Email, user.FullName, user.Roles));
    }
}
