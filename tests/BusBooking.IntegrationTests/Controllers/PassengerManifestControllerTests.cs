using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.Domain.Enums;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class PassengerManifestControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PassengerManifestControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPassengerManifest_ReturnsAllPassengersSortedBySeatNumber()
    {
        var scenario = await SeedManifestScenarioAsync();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/trips/{scenario.TripId}/passenger-manifest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(3);
        body.Data!.Select(e => e.SeatNumber).Should().BeInAscendingOrder();
        body.Data!.Select(e => e.PassengerName).Should().Contain(["Nimal", "Kamal", "Sunil"]);
    }

    [Fact]
    public async Task GetPassengerManifest_FilteredByBookingStatus_ReturnsOnlyConfirmed()
    {
        var scenario = await SeedManifestScenarioAsync();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/trips/{scenario.TripId}/passenger-manifest?bookingStatus=Confirmed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(1);
        body.Data!.Single().PassengerName.Should().Be("Nimal");
        body.Data!.Single().BookingStatus.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task GetPassengerManifest_FilteredByPickupStop_ReturnsOnlyMatching()
    {
        var scenario = await SeedManifestScenarioAsync();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/trips/{scenario.TripId}/passenger-manifest?pickupStopId={scenario.StopIds[1]}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(1);
        body.Data!.Single().PassengerName.Should().Be("Kamal");
    }

    [Fact]
    public async Task GetPassengerManifest_WithSearchTerm_ReturnsOnlyMatching()
    {
        var scenario = await SeedManifestScenarioAsync();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/trips/{scenario.TripId}/passenger-manifest?searchTerm=Sunil");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(1);
        body.Data!.Single().PassengerName.Should().Be("Sunil");
    }

    [Fact]
    public async Task GetPassengerManifest_ForNonExistentTrip_ReturnsNotFound()
    {
        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/trips/{Guid.NewGuid()}/passenger-manifest");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPassengerManifest_AsGuest_ReturnsUnauthorized()
    {
        var scenario = await SeedManifestScenarioAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/trips/{scenario.TripId}/passenger-manifest");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ManifestScenario(Guid TripId, IReadOnlyList<Guid> StopIds);

    /// <summary>
    /// Route A(0) -> B(1) -> C(2), three seats, three single-passenger bookings:
    /// Nimal: seat 1, A -> B, payment confirmed (BookingStatus.Confirmed).
    /// Kamal: seat 2, B -> C, left Pending.
    /// Sunil: seat 3, A -> C, left Pending.
    /// </summary>
    private async Task<ManifestScenario> SeedManifestScenarioAsync()
    {
        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{suffix}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new { seatNumber = $"{i:00}", row = 0, column = i, positionType = "Seat" });
        }

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PatchAsJsonAsync($"/api/buses/{bus.Id}/seat-layout", new { seatLayoutId = layout.Id });

        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from = "A", to = "C" });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;
        var stopIds = new List<Guid>();
        foreach (var stopName in new[] { "A", "B", "C" })
        {
            var stopResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName, allowPickup = true, allowDropOff = true });
            var stop = (await stopResponse.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default))!.Data!;
            stopIds.Add(stop.Id);
        }
        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        var tripResponse = await _client.PostAsJsonAsync("/api/trips", new
        {
            routeId = route.Id,
            busId = bus.Id,
            tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd"),
            departureTime = "08:00:00",
            expectedArrivalTime = "17:00:00",
            fare = 3500m
        });
        var trip = (await tripResponse.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PatchAsync($"/api/trips/{trip.Id}/schedule", null);

        var seatsResponse = await _client.GetAsync($"/api/trips/{trip.Id}/seats");
        var tripSeatIds = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!
            .OrderBy(s => s.SeatNumber)
            .Select(s => s.TripSeatId)
            .ToList();

        _client.DefaultRequestHeaders.Authorization = null;

        var nimalBookingId = await CreateSinglePassengerBookingAsync(trip.Id, tripSeatIds[0], "Nimal", stopIds[0], stopIds[1]);
        await CreateSinglePassengerBookingAsync(trip.Id, tripSeatIds[1], "Kamal", stopIds[1], stopIds[2]);
        await CreateSinglePassengerBookingAsync(trip.Id, tripSeatIds[2], "Sunil", stopIds[0], stopIds[2]);

        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId = nimalBookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);

        return new ManifestScenario(trip.Id, stopIds);
    }

    private async Task<Guid> CreateSinglePassengerBookingAsync(Guid tripId, Guid tripSeatId, string passengerName, Guid pickupStopId, Guid dropOffStopId)
    {
        var lockResponse = await _client.PostAsync($"/api/trips/{tripId}/seats/{tripSeatId}/lock", null);
        var lockId = (await lockResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default))!.Data!.LockId;

        var bookingResponse = await _client.PostAsJsonAsync("/api/bookings", new
        {
            tripId,
            passengers = new[]
            {
                new
                {
                    fullName = passengerName,
                    phoneNumber = "0771234567",
                    gender = "Male",
                    pickupStopId,
                    dropOffStopId,
                    tripSeatId,
                    lockId
                }
            }
        });

        return (await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!.Id;
    }
}
