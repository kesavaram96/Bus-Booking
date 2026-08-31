using System.Net;
using System.Net.Http.Json;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

/// <summary>Uses RateLimitedWebApplicationFactory (PermitLimit: 2 per minute for login), not
/// CustomWebApplicationFactory, whose login limit is deliberately raised so the rest of this
/// suite's request volume never trips it — see that factory's own doc comment.</summary>
public class RateLimitingTests : IClassFixture<RateLimitedWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(RateLimitedWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ExceedingThePermitLimit_ReturnsTooManyRequests()
    {
        var body = new { usernameOrEmail = "nobody@example.com", password = "wrong-password" };

        var first = await _client.PostAsJsonAsync("/api/auth/login", body);
        var second = await _client.PostAsJsonAsync("/api/auth/login", body);
        var third = await _client.PostAsJsonAsync("/api/auth/login", body);

        first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
