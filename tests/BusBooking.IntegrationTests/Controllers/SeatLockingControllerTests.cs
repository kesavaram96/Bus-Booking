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
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

/// <summary>
/// Runs against a real, locally-built Redis instance (see README) — the whole point of this
/// phase is atomic cross-process coordination, which a fake/in-memory substitute can't
/// meaningfully verify, especially the concurrency test below.
/// </summary>
public class SeatLockingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SeatLockingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LockSeat_OnAvailableSeat_ReturnsLockExpiringInTenMinutes()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null; // guest — no account

        var before = DateTime.UtcNow;
        var response = await LockAsync(tripSeatId, tripId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default);
        body!.Data!.LockId.Should().NotBeNullOrWhiteSpace();
        body.Data.LockedUntil.Should().BeCloseTo(before.AddMinutes(10), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task LockSeat_OnAlreadyHeldSeat_ReturnsBadRequest()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var first = await LockAsync(tripSeatId, tripId);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await LockAsync(tripSeatId, tripId);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LockSeat_OnBlockedSeat_ReturnsBadRequest()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();

        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PatchAsync($"/api/trips/{tripId}/seats/{tripSeatId}/block", null);

        _client.DefaultRequestHeaders.Authorization = null;
        var response = await LockAsync(tripSeatId, tripId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LockSeat_TenConcurrentAttemptsOnSameSeat_ExactlyOneSucceeds()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();

        // Ten independent, unauthenticated clients racing for the same seat simultaneously —
        // this is what the atomic Redis SET NX is actually for. Clients must outlive the
        // requests (created up front, disposed only after every response has arrived) —
        // disposing one mid-request aborts its connection.
        var clients = Enumerable.Range(0, 10).Select(_ => _factory.CreateClient()).ToList();
        try
        {
            var tasks = clients.Select(client => client.PostAsync($"/api/trips/{tripId}/seats/{tripSeatId}/lock", null));
            var responses = await Task.WhenAll(tasks);

            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
            responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(9);
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    [Fact]
    public async Task UnlockSeat_WithCorrectLockId_ReleasesSeatForAnotherCustomer()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var lockResponse = await LockAsync(tripSeatId, tripId);
        var lockId = (await lockResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default))!.Data!.LockId;

        var unlockResponse = await _client.PostAsJsonAsync(
            $"/api/trips/{tripId}/seats/{tripSeatId}/unlock", new { lockId });
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Freed — someone else can now lock it.
        var relockResponse = await LockAsync(tripSeatId, tripId);
        relockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnlockSeat_WithWrongLockId_ReturnsBadRequestAndKeepsSeatHeld()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        await LockAsync(tripSeatId, tripId);

        var wrongUnlock = await _client.PostAsJsonAsync(
            $"/api/trips/{tripId}/seats/{tripSeatId}/unlock", new { lockId = "not-the-real-token" });
        wrongUnlock.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Still held — a competing lock attempt must still fail.
        var competingLock = await LockAsync(tripSeatId, tripId);
        competingLock.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnlockSeat_NeverLocked_IsIdempotentSuccess()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            $"/api/trips/{tripId}/seats/{tripSeatId}/unlock", new { lockId = "whatever" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockSeat_AfterUnderlyingRedisKeyExpires_AllowsReLocking()
    {
        var (tripId, tripSeatId) = await SeedAvailableTripSeatWithIdsAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var first = await LockAsync(tripSeatId, tripId);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Simulate the 10-minute TTL having elapsed, without waiting for it in real time —
        // deleting the key is exactly what Redis itself does on expiry.
        var multiplexer = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var db = multiplexer.GetDatabase();
        await db.KeyDeleteAsync($"BusBooking:Dev:seatlock:{tripSeatId}");

        var second = await LockAsync(tripSeatId, tripId);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> LockAsync(Guid tripSeatId, Guid tripId) =>
        _client.PostAsync($"/api/trips/{tripId}/seats/{tripSeatId}/lock", null);

    private async Task<(Guid TripId, Guid TripSeatId)> SeedAvailableTripSeatWithIdsAsync()
    {
        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

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

        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from = "A", to = "B" });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "A", allowPickup = true, allowDropOff = true });
        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "B", allowPickup = true, allowDropOff = true });
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

        var seatsResponse = await _client.GetAsync($"/api/trips/{trip.Id}/seats");
        var seats = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!;

        return (trip.Id, seats.Single().TripSeatId);
    }
}
