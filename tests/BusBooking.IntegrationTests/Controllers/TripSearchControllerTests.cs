using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class TripSearchControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TripSearchControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithoutAnyToken_ReturnsMatchingTrip()
    {
        var (from, to, tripDate, _) = await SeedScheduledTripAsync();
        _client.DefaultRequestHeaders.Authorization = null; // guest — no account at all

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_ResponseNeverContainsBusRegistrationOrDriverFields()
    {
        var (from, to, tripDate, _) = await SeedScheduledTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate:yyyy-MM-dd}");
        var json = await response.Content.ReadAsStringAsync();

        // Case-insensitive substring check — the restriction must hold at the wire level, not
        // just "the DTO doesn't have a property for it" (which a future refactor could break).
        json.ToLowerInvariant().Should().NotContain("registrationnumber");
        json.ToLowerInvariant().Should().NotContain("busid");
        json.ToLowerInvariant().Should().NotContain("driverid");
        json.ToLowerInvariant().Should().NotContain("drivername");
    }

    [Fact]
    public async Task Search_ExcludesDraftTrip()
    {
        var (from, to, tripDate, _) = await SeedTripAsync(schedule: false);
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate:yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesCancelledTrip()
    {
        var (from, to, tripDate, tripId) = await SeedScheduledTripAsync();

        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await _client.PatchAsync($"/api/trips/{tripId}/cancel", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate:yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesDifferentDate()
    {
        var (from, to, tripDate, _) = await SeedScheduledTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate.AddDays(1):yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesDifferentRoute()
    {
        var (from, to, tripDate, _) = await SeedScheduledTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={to}&to={from}&date={tripDate:yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ReturnsPickupPointsForTheRoute()
    {
        var (from, to, tripDate, _) = await SeedScheduledTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from={from}&to={to}&date={tripDate:yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripSearchResultDto>>>(TestJsonOptions.Default);
        var trip = body!.Data!.Items.Single();
        trip.PickupPoints.Should().Contain(p => p.StopName == from);
    }

    [Fact]
    public async Task Search_WithSameFromAndTo_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/search?from=Colombo&to=Colombo&date={DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Creates a route/bus/trip and schedules it (or leaves it Draft when schedule: false).
    /// Returns (From, To, TripDate, TripId).
    /// </summary>
    private async Task<(string From, string To, DateOnly TripDate, Guid TripId)> SeedTripAsync(bool schedule)
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var from = $"From-{suffix}";
        var to = $"To-{suffix}";

        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from, to });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;

        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = from, allowPickup = true, allowDropOff = true });
        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = to, allowPickup = true, allowDropOff = true });
        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{suffix}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new { seatNumber = "01", row = 0, column = 0, positionType = "Seat" });

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PatchAsJsonAsync($"/api/buses/{bus.Id}/seat-layout", new { seatLayoutId = layout.Id });

        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

        var tripResponse = await _client.PostAsJsonAsync("/api/trips", new
        {
            routeId = route.Id,
            busId = bus.Id,
            tripDate = tripDate.ToString("yyyy-MM-dd"),
            departureTime = "08:00:00",
            expectedArrivalTime = "17:00:00",
            fare = 3500m
        });
        var trip = (await tripResponse.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!;

        if (schedule)
        {
            await _client.PatchAsync($"/api/trips/{trip.Id}/schedule", null);
        }

        return (from, to, tripDate, trip.Id);
    }

    private Task<(string From, string To, DateOnly TripDate, Guid TripId)> SeedScheduledTripAsync() => SeedTripAsync(schedule: true);
}
