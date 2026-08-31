namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// Owns the full refresh-token lifecycle: issuing, rotating (single-use, atomically
/// replaced on refresh) and revoking. The database remains the source of truth —
/// tokens are stored hashed, never in plain text.
/// </summary>
public interface IRefreshTokenService
{
    Task<(string RawToken, DateTime ExpiresAtUtc)> IssueAsync(Guid userId, CancellationToken cancellationToken);

    Task<(bool Success, Guid? UserId, string? NewRawToken, DateTime? NewExpiresAtUtc)> RotateAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken);

    Task RevokeAsync(string rawRefreshToken, CancellationToken cancellationToken);
}
