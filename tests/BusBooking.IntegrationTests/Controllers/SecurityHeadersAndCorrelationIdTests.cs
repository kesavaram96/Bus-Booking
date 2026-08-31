using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class SecurityHeadersAndCorrelationIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersAndCorrelationIdTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnyResponse_IncludesTheExpectedSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/routes/active");

        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle("no-referrer");
        response.Headers.Contains("Permissions-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_GeneratesOneOnTheResponse()
    {
        var response = await _client.GetAsync("/api/routes/active");

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        Guid.TryParse(values!.Single(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithCorrelationIdHeader_EchoesItBackUnchanged()
    {
        var incomingCorrelationId = $"test-{Guid.NewGuid():N}";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/routes/active");
        request.Headers.Add("X-Correlation-Id", incomingCorrelationId);

        var response = await _client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle(incomingCorrelationId);
    }
}
