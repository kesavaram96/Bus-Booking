using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Audit.DTOs;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Constants;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class AuditLogsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ValidPassword = "P@ssw0rd123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditLogsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBus_ProducesAnAuditLogEntryVisibleToAnAdmin()
    {
        var opsToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.OperationsManager);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);

        var registrationNumber = $"NB-{Guid.NewGuid():N}"[..12];
        var createResponse = await _client.PostAsJsonAsync("/api/buses", new { registrationNumber, busType = "Normal" });
        var bus = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;

        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync($"/api/audit-logs?entityName=Bus&entityId={bus.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<AuditLogDto>>>(TestJsonOptions.Default);
        var entry = body!.Data!.Items.Single();
        entry.Action.Should().Be("CreateBus");
        entry.EntityName.Should().Be("Bus");
        entry.EntityId.Should().Be(bus.Id);
        entry.NewValues.Should().Contain(bus.RegistrationNumber);
    }

    [Fact]
    public async Task Login_AuditLogEntry_NeverContainsTheRealAccessOrRefreshToken()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        _client.DefaultRequestHeaders.Authorization = null;
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Audit Test Customer",
            email,
            phoneNumber = "+94770000000",
            password = ValidPassword
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = email, password = ValidPassword });
        var authResult = (await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(TestJsonOptions.Default))!.Data!;

        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Filtered by EntityId (the logged-in user, resolved from the response), not UserId —
        // Login is anonymous when it's made, so ICurrentUserService has no JWT to read a
        // "who's acting" claim from yet; UserId legitimately stays null for this one action.
        var response = await _client.GetAsync($"/api/audit-logs?action=Login&entityId={authResult.User.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<AuditLogDto>>>(TestJsonOptions.Default);
        var entry = body!.Data!.Items.Single();
        entry.UserId.Should().BeNull();
        entry.NewValues.Should().NotContain(authResult.AccessToken);
        entry.NewValues.Should().NotContain(authResult.RefreshToken);
        entry.NewValues.Should().Contain("REDACTED");
    }

    [Fact]
    public async Task GetAuditLogs_AsBookingStaff_ReturnsForbidden()
    {
        // RequireAdminOrAbove is deliberately stricter than RequireBookingStaff.
        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_AsGuest_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
