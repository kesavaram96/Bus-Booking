using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.Domain.Enums;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class TripSeatsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TripSeatsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTrip_GeneratesTripSeatsOnlyForActivePhysicalSeats()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();

        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        await AddSeatAsync(layoutId, "02", 0, 1, "Seat");
        var inactiveSeat = await AddSeatAsync(layoutId, "03", 0, 2, "Seat");
        await _client.PatchAsync($"/api/seat-layouts/{layoutId}/seats/{inactiveSeat}/deactivate", null);
        await AddSeatAsync(layoutId, "D", 1, 0, "Driver");

        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);

        var response = await _client.GetAsync($"/api/trips/{tripId}/seats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default);
        body!.Data!.Should().HaveCount(2);
        body.Data.Should().OnlyContain(s => s.Status == TripSeatStatus.Available);
    }

    [Fact]
    public async Task GetSeatMap_WithoutAnyToken_ReturnsSeatsWithLayoutDimensions()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync(rows: 5, columns: 2);
        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/trips/{tripId}/seat-map");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatMapDto>>(TestJsonOptions.Default);
        body!.Data!.Rows.Should().Be(5);
        body.Data.Columns.Should().Be(2);
        body.Data.Seats.Should().ContainSingle().Which.SeatNumber.Should().Be("01");
    }

    [Fact]
    public async Task GetSeatMap_NeverContainsBookingIdField()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/trips/{tripId}/seat-map");
        var json = await response.Content.ReadAsStringAsync();

        json.ToLowerInvariant().Should().NotContain("bookingid");
    }

    [Fact]
    public async Task BlockSeat_ThenUnblock_TogglesStatus()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);
        var tripSeatId = await GetFirstTripSeatIdAsync(tripId);

        var blockResponse = await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/block", null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var blocked = await blockResponse.Content.ReadFromJsonAsync<ApiResponse<TripSeatDto>>(TestJsonOptions.Default);
        blocked!.Data!.Status.Should().Be(TripSeatStatus.Blocked);

        var unblockResponse = await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/unblock", null);
        unblockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unblocked = await unblockResponse.Content.ReadFromJsonAsync<ApiResponse<TripSeatDto>>(TestJsonOptions.Default);
        unblocked!.Data!.Status.Should().Be(TripSeatStatus.Available);
    }

    [Fact]
    public async Task BlockSeat_AlreadyBlocked_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);
        var tripSeatId = await GetFirstTripSeatIdAsync(tripId);

        await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/block", null);
        var response = await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/block", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BlockSeat_AsBookingStaff_ReturnsForbidden()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busId);
        var tripSeatId = await GetFirstTripSeatIdAsync(tripId);

        var bookingStaffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bookingStaffToken);

        var response = await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/block", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignBus_RegeneratesTripSeatsForNewBusLayout()
    {
        await AuthenticateAsAdminAsync();
        var layoutA = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutA, "01", 0, 0, "Seat");
        var busA = await CreateBusWithLayoutAsync(layoutA);

        var layoutB = await CreateSeatLayoutAsync();
        await AddSeatAsync(layoutB, "01", 0, 0, "Seat");
        await AddSeatAsync(layoutB, "02", 0, 1, "Seat");
        var busB = await CreateBusWithLayoutAsync(layoutB);

        var routeId = await CreateActiveRouteAsync();
        var tripId = await CreateTripAsync(routeId, busA);

        var beforeResponse = await _client.GetAsync($"/api/trips/{tripId}/seats");
        var before = await beforeResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default);
        before!.Data!.Should().HaveCount(1);

        await _client.PatchAsJsonAsync($"/api/trips/{tripId}/bus", new { busId = busB });

        var afterResponse = await _client.GetAsync($"/api/trips/{tripId}/seats");
        var after = await afterResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default);
        after!.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveSeat_UsedOnATrip_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var layoutId = await CreateSeatLayoutAsync();
        var seatId = await AddSeatAsync(layoutId, "01", 0, 0, "Seat");
        var busId = await CreateBusWithLayoutAsync(layoutId);
        var routeId = await CreateActiveRouteAsync();
        await CreateTripAsync(routeId, busId);

        var response = await _client.DeleteAsync($"/api/seat-layouts/{layoutId}/seats/{seatId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> GetFirstTripSeatIdAsync(Guid tripId)
    {
        var response = await _client.GetAsync($"/api/trips/{tripId}/seats");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default);
        return body!.Data!.Single().TripSeatId;
    }

    private async Task<Guid> CreateTripAsync(Guid routeId, Guid busId)
    {
        var response = await _client.PostAsJsonAsync("/api/trips", new
        {
            routeId,
            busId,
            tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd"),
            departureTime = "08:00:00",
            expectedArrivalTime = "17:00:00",
            fare = 3500m
        });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        return body!.Data!.Id;
    }

    private async Task<Guid> CreateActiveRouteAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from = $"From-{suffix}", to = $"To-{suffix}" });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;

        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "A", allowPickup = true, allowDropOff = true });
        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "B", allowPickup = true, allowDropOff = true });
        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        return route.Id;
    }

    private async Task<Guid> CreateSeatLayoutAsync(int rows = 10, int columns = 4)
    {
        var response = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{Guid.NewGuid():N}", rows, columns });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default);
        return body!.Data!.Id;
    }

    private async Task<Guid> AddSeatAsync(Guid layoutId, string seatNumber, int row, int column, string positionType)
    {
        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layoutId}/seats", new
        {
            seatNumber,
            row,
            column,
            positionType
        });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>(TestJsonOptions.Default);
        return body!.Data!.Id;
    }

    private async Task<Guid> CreateBusWithLayoutAsync(Guid layoutId)
    {
        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;

        await _client.PatchAsJsonAsync($"/api/buses/{bus.Id}/seat-layout", new { seatLayoutId = layoutId });

        return bus.Id;
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
