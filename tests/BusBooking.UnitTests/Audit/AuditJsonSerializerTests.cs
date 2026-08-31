using BusBooking.Application.Common.Auditing;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Audit;

public class AuditJsonSerializerTests
{
    private sealed record LoginLikeResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, string UserEmail);

    private sealed record WithPassword(string Password, string Username);

    [Fact]
    public void Serialize_RedactsStringPropertiesNamedLikeASecret()
    {
        var response = new LoginLikeResponse("real-access-token", DateTime.UtcNow, "real-refresh-token", "someone@example.com");

        var json = AuditJsonSerializer.Serialize(response);

        json.Should().NotContain("real-access-token");
        json.Should().NotContain("real-refresh-token");
        json.Should().Contain("***REDACTED***");
        json.Should().Contain("someone@example.com");
    }

    [Fact]
    public void Serialize_DoesNotThrowForNonStringPropertyWhoseNameContainsASensitiveFragment()
    {
        // AccessTokenExpiresAtUtc is a DateTime, not a string — matching on "token" here must
        // not try to swap in a string value for it.
        var response = new LoginLikeResponse("token", DateTime.UtcNow, "token", "someone@example.com");

        var act = () => AuditJsonSerializer.Serialize(response);

        act.Should().NotThrow();
    }

    [Fact]
    public void Serialize_RedactsPassword()
    {
        var response = new WithPassword("SuperSecret123!", "someuser");

        var json = AuditJsonSerializer.Serialize(response);

        json.Should().NotContain("SuperSecret123!");
        json.Should().Contain("someuser");
    }

    [Fact]
    public void Serialize_WithNull_ReturnsNull()
    {
        AuditJsonSerializer.Serialize(null).Should().BeNull();
    }
}
