using BusBooking.Application.Common.Models;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// Pure token cryptography — no persistence. Refresh token lifecycle/persistence is
/// handled separately by <see cref="IRefreshTokenService"/>.
/// </summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(AuthenticatedUserDto user);

    string GenerateRefreshToken();
}
