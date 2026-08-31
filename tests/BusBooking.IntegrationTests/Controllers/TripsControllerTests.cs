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
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Infrastructure.Persistence.DbContext;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class TripsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TripsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsDraftTrip()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();

        var response = await CreateTripResponseAsync(routeId, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(TripStatus.Draft);
        body.Data.RouteId.Should().Be(routeId);
        body.Data.BusId.Should().Be(busId);
    }

    [Fact]
    public async Task Create_WithInactiveRoute_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{Guid.NewGuid():N}", from = "A", to = "B" });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;
        var busId = await CreateActiveBusAsync();

        var response = await CreateTripResponseAsync(route.Id, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithBusMissingSeatLayout_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;

        var response = await CreateTripResponseAsync(routeId, bus.Id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithInactiveBus_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        await _client.PatchAsync($"/api/buses/{busId}/deactivate", null);

        var response = await CreateTripResponseAsync(routeId, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_OverlappingTripForSameBus_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var first = await CreateTripResponseAsync(routeId, busId, tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(17));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Overlaps the first trip's 08:00-17:00 window.
        var response = await CreateTripResponseAsync(routeId, busId, tripDate, TimeSpan.FromHours(10), TimeSpan.FromHours(19));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonOverlappingTripForSameBusSameDay_Succeeds()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var first = await CreateTripResponseAsync(routeId, busId, tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(11));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await CreateTripResponseAsync(routeId, busId, tripDate, TimeSpan.FromHours(12), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_OvernightTripOverlappingNextMorning_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        var day1 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var day2 = day1.AddDays(1);

        // Overnight: departs day1 20:00, arrives day2 05:00.
        var overnight = await CreateTripResponseAsync(routeId, busId, day1, TimeSpan.FromHours(20), TimeSpan.FromHours(5));
        overnight.StatusCode.Should().Be(HttpStatusCode.Created);

        // A trip on day2 starting at 03:00 falls inside the overnight trip's window.
        var response = await CreateTripResponseAsync(routeId, busId, day2, TimeSpan.FromHours(3), TimeSpan.FromHours(9));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AsBookingStaff_ReturnsForbidden()
    {
        var routeId = await CreateRouteAsAdminAsync();
        var busId = await CreateBusAsAdminAsync();

        await AuthenticateAsAsync(Roles.BookingStaff);
        var response = await CreateTripResponseAsync(routeId, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullLifecycle_ScheduleBoardDepartComplete_Succeeds()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();

        (await _client.PatchAsync($"/api/trips/{tripId}/schedule", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PatchAsync($"/api/trips/{tripId}/boarding", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PatchAsync($"/api/trips/{tripId}/departed", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PatchAsync($"/api/trips/{tripId}/completed", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var trip = await GetTripAsync(tripId);
        trip.Status.Should().Be(TripStatus.Completed);
    }

    [Fact]
    public async Task MarkBoarding_WhileStillDraft_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();

        var response = await _client.PatchAsync($"/api/trips/{tripId}/boarding", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_ThenCancelAgain_SecondCallReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();

        (await _client.PatchAsync($"/api/trips/{tripId}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await _client.PatchAsync($"/api/trips/{tripId}/cancel", null);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_CascadesToRefundAConfirmedPaidBookingOnThatTrip()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();

        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{Guid.NewGuid():N}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new { seatNumber = "01", row = 0, column = 0, positionType = "Seat" });

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var busId = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!.Id;
        await _client.PatchAsJsonAsync($"/api/buses/{busId}/seat-layout", new { seatLayoutId = layout.Id });

        var tripResponse = await CreateTripResponseAsync(
            routeId, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));
        var trip = (await tripResponse.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PatchAsync($"/api/trips/{trip.Id}/schedule", null);

        var seatsResponse = await _client.GetAsync($"/api/trips/{trip.Id}/seats");
        var tripSeatId = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!.Single().TripSeatId;
        var routeStopsResponse = await _client.GetAsync($"/api/routes/{routeId}");
        var route = (await routeStopsResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;
        var pickupStopId = route.Stops.OrderBy(s => s.StopOrder).First().Id;
        var dropOffStopId = route.Stops.OrderBy(s => s.StopOrder).Last().Id;

        _client.DefaultRequestHeaders.Authorization = null;
        var lockResponse = await _client.PostAsync($"/api/trips/{trip.Id}/seats/{tripSeatId}/lock", null);
        var lockId = (await lockResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default))!.Data!.LockId;

        var bookingResponse = await _client.PostAsJsonAsync("/api/bookings", new
        {
            tripId = trip.Id,
            passengers = new[]
            {
                new
                {
                    fullName = "Someone",
                    phoneNumber = "0771234567",
                    gender = "Male",
                    pickupStopId,
                    dropOffStopId,
                    tripSeatId,
                    lockId
                }
            }
        });
        var bookingId = (await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!.Id;

        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);

        await AuthenticateAsAsync(Roles.OperationsManager);
        var cancelResponse = await _client.PatchAsync($"/api/trips/{trip.Id}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var bookingAfter = await _client.GetAsync($"/api/bookings/{bookingId}");
        var bookingAfterBody = (await bookingAfter.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!;

        bookingAfterBody.Status.Should().Be(BookingStatus.Refunded);
        bookingAfterBody.CancelledBy.Should().BeNull();
        bookingAfterBody.CancellationReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UpdateTrip_AfterDeparted_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();
        await _client.PatchAsync($"/api/trips/{tripId}/schedule", null);
        await _client.PatchAsync($"/api/trips/{tripId}/boarding", null);
        await _client.PatchAsync($"/api/trips/{tripId}/departed", null);

        var trip = await GetTripAsync(tripId);
        var response = await _client.PutAsJsonAsync($"/api/trips/{tripId}", new
        {
            tripDate = trip.TripDate.ToString("yyyy-MM-dd"),
            departureTime = trip.DepartureTime.ToString(),
            expectedArrivalTime = trip.ExpectedArrivalTime.ToString(),
            fare = 4200m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignDriver_WithActiveDriver_Succeeds()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();
        var driverId = await SeedActiveDriverAsync();

        var response = await _client.PatchAsJsonAsync($"/api/trips/{tripId}/driver", new { driverId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        body!.Data!.DriverId.Should().Be(driverId);
    }

    [Fact]
    public async Task AssignDriver_WithInactiveDriver_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();
        var driverId = await SeedActiveDriverAsync(isActive: false);

        var response = await _client.PatchAsJsonAsync($"/api/trips/{tripId}/driver", new { driverId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveDriver_ClearsAssignedDriver()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var tripId = await CreateTripAsync();
        var driverId = await SeedActiveDriverAsync();
        await _client.PatchAsJsonAsync($"/api/trips/{tripId}/driver", new { driverId });

        var response = await _client.DeleteAsync($"/api/trips/{tripId}/driver");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        body!.Data!.DriverId.Should().BeNull();
    }

    [Fact]
    public async Task AssignBus_ToOverlappingBus_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busA = await CreateActiveBusAsync();
        var busB = await CreateActiveBusAsync();
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // busB is already committed to this exact window.
        await CreateTripResponseAsync(routeId, busB, tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(17));

        var tripOnBusA = await CreateTripResponseAsync(routeId, busA, tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(17));
        var tripAId = (await tripOnBusA.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!.Id;

        var response = await _client.PatchAsJsonAsync($"/api/trips/{tripAId}/bus", new { busId = busB });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTrips_FilteredByRouteAndFromDate_ReturnsOnlyMatching()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);

        var created = await CreateTripResponseAsync(routeId, busId, tripDate, TimeSpan.FromHours(8), TimeSpan.FromHours(17));
        var tripId = (await created.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!.Id;

        var response = await _client.GetAsync($"/api/trips?routeId={routeId}&fromDate={tripDate:yyyy-MM-dd}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<TripDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().Contain(t => t.Id == tripId);
        body.Data.Items.Should().OnlyContain(t => t.RouteId == routeId);
    }

    private async Task<Guid> CreateTripAsync()
    {
        var routeId = await CreateActiveRouteAsync();
        var busId = await CreateActiveBusAsync();
        var response = await CreateTripResponseAsync(routeId, busId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), TimeSpan.FromHours(8), TimeSpan.FromHours(17));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        return body!.Data!.Id;
    }

    private Task<HttpResponseMessage> CreateTripResponseAsync(
        Guid routeId, Guid busId, DateOnly tripDate, TimeSpan departureTime, TimeSpan expectedArrivalTime) =>
        _client.PostAsJsonAsync("/api/trips", new
        {
            routeId,
            busId,
            tripDate = tripDate.ToString("yyyy-MM-dd"),
            departureTime = departureTime.ToString(),
            expectedArrivalTime = expectedArrivalTime.ToString(),
            fare = 3500m
        });

    private async Task<TripDto> GetTripAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/trips/{id}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<Guid> CreateActiveRouteAsync()
    {
        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{Guid.NewGuid():N}", from = "A", to = "B" });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;

        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "A", allowPickup = true, allowDropOff = true });
        await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "B", allowPickup = true, allowDropOff = true });
        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        return route.Id;
    }

    private async Task<Guid> CreateRouteAsAdminAsync()
    {
        await AuthenticateAsAsync(Roles.Admin);
        return await CreateActiveRouteAsync();
    }

    private async Task<Guid> CreateActiveBusAsync()
    {
        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{Guid.NewGuid():N}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;

        await _client.PatchAsJsonAsync($"/api/buses/{bus.Id}/seat-layout", new { seatLayoutId = layout.Id });

        return bus.Id;
    }

    private async Task<Guid> CreateBusAsAdminAsync()
    {
        await AuthenticateAsAsync(Roles.Admin);
        return await CreateActiveBusAsync();
    }

    /// <summary>
    /// There is no /api/drivers endpoint — Driver's fields were fully specified back in
    /// Phase 02 alongside Bus/SeatLayout/Route, but the source doc never gives Driver its own
    /// CRUD phase/prompt. Seed directly against the DbContext instead, same as SeatLayout was
    /// seeded directly in BusesControllerTests before Phase 05 existed.
    /// </summary>
    private async Task<Guid> SeedActiveDriverAsync(bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var driver = new Driver($"Driver {Guid.NewGuid():N}", "0771234567", $"LIC-{Guid.NewGuid():N}"[..12], new DateOnly(2030, 1, 1));
        if (!isActive)
        {
            driver.Deactivate();
        }

        context.Drivers.Add(driver);
        await context.SaveChangesAsync();

        return driver.Id;
    }

    private async Task AuthenticateAsAsync(string role)
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
