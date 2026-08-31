namespace BusBooking.Infrastructure.Identity;

/// <summary>
/// Bound from the "Jwt" configuration section. Secret must come from user-secrets or
/// environment variables — never committed (see appsettings.*.json).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
