using System.Security.Cryptography;
using System.Text;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusBooking.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rawToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        _dbContext.RefreshTokens.Add(new RefreshToken(userId, Hash(rawToken), expiresAtUtc));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (rawToken, expiresAtUtc);
    }

    public async Task<(bool Success, Guid? UserId, string? NewRawToken, DateTime? NewExpiresAtUtc)> RotateAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var hash = Hash(rawRefreshToken);
        var existing = await _dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return (false, null, null, null);
        }

        var newRawToken = _jwtTokenService.GenerateRefreshToken();
        var newExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var newHash = Hash(newRawToken);

        existing.Revoke(newHash);
        _dbContext.RefreshTokens.Add(new RefreshToken(existing.UserId, newHash, newExpiresAtUtc));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (true, existing.UserId, newRawToken, newExpiresAtUtc);
    }

    public async Task RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(rawRefreshToken);
        var existing = await _dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (existing is not null && existing.IsActive)
        {
            existing.Revoke();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
