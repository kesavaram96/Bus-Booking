using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class HealthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadiness_ChecksTheRealDatabaseAndRedis_ReportsHealthy()
    {
        // /health (unlike /api/health above) actually calls the database and Redis — the test
        // host's DbContext is EF InMemory (always reachable) but Redis is the real instance
        // already used for seat locking throughout this suite, so a Healthy result here is a
        // genuine PING round trip, not a mocked one.
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Healthy");

        var checks = body.GetProperty("checks").EnumerateArray().ToList();
        checks.Should().Contain(c => c.GetProperty("name").GetString() == "database" && c.GetProperty("status").GetString() == "Healthy");
        checks.Should().Contain(c => c.GetProperty("name").GetString() == "redis" && c.GetProperty("status").GetString() == "Healthy");
    }

    [Fact]
    public async Task SwaggerDocument_GeneratesSuccessfully()
    {
        // Regression guard: OpenAPI generation can silently break on record DTOs, nullable
        // enum query parameters, etc. — this would only surface by actually hitting the doc.
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("/api/buses");
    }
}
