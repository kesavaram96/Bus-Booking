using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.Domain.Enums;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class ReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ValidPassword = "P@ssw0rd123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDailyBookings_ReturnsTodayWithCorrectCountAndTotal()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/daily-bookings?routeId={scenario.Route1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<DailyBookingReportEntryDto>>>(TestJsonOptions.Default);
        var today = body!.Data!.Single(e => e.Date == DateOnly.FromDateTime(DateTime.UtcNow));
        today.BookingCount.Should().Be(2);
        today.TotalAmount.Should().Be(7000m);
    }

    [Fact]
    public async Task GetDailyBookings_FilteredByFutureFromDate_ExcludesToday()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var response = await _client.GetAsync($"/api/reports/daily-bookings?routeId={scenario.Route1Id}&fromDate={tomorrow:yyyy-MM-dd}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<DailyBookingReportEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDailyBookings_FilteredByStatus_ReturnsOnlyMatching()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/daily-bookings?routeId={scenario.Route1Id}&status=Pending");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<DailyBookingReportEntryDto>>>(TestJsonOptions.Default);
        body!.Data!.Single().BookingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTripPassengers_FilteredByTrip_ReturnsOnlyThatTripsPassengers()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/trip-passengers?tripId={scenario.Trip1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerReportEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(2);
        body.Data!.Should().OnlyContain(p => p.TripId == scenario.Trip1Id);
    }

    [Fact]
    public async Task GetTripPassengers_FilteredByRouteAndStatus_ReturnsOnlyConfirmed()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/trip-passengers?routeId={scenario.Route1Id}&status=Confirmed");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerReportEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().ContainSingle();
        body.Data!.Single().BookingStatus.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task GetRevenue_OnlyCountsPaidPayments()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var route1Response = await _client.GetAsync($"/api/reports/revenue?routeId={scenario.Route1Id}");
        var route1Body = await route1Response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RevenueReportEntryDto>>>(TestJsonOptions.Default);
        var today = route1Body!.Data!.Single(e => e.Date == DateOnly.FromDateTime(DateTime.UtcNow));
        today.PaymentCount.Should().Be(1);
        today.TotalRevenue.Should().Be(3500m);

        // Route2's only payment was later refunded, so it must not appear as revenue at all.
        var route2Response = await _client.GetAsync($"/api/reports/revenue?routeId={scenario.Route2Id}");
        var route2Body = await route2Response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RevenueReportEntryDto>>>(TestJsonOptions.Default);
        route2Body!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCancellations_ReturnsRefundedBookingWithReasonAndCanceller()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/cancellations?routeId={scenario.Route2Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CancellationReportEntryDto>>>(TestJsonOptions.Default);
        var entry = body!.Data!.Single();
        entry.BookingId.Should().Be(scenario.CancelledBookingId);
        entry.Status.Should().Be(BookingStatus.Refunded);
        entry.CancellationReason.Should().Be("Testing reports.");
        entry.CancelledBy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSeatOccupancy_ReflectsBookedVsTotalSeatsExcludingRefunded()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var trip1Response = await _client.GetAsync($"/api/reports/seat-occupancy?tripId={scenario.Trip1Id}");
        var trip1Body = await trip1Response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SeatOccupancyReportEntryDto>>>(TestJsonOptions.Default);
        var trip1 = trip1Body!.Data!.Single();
        trip1.TotalSeats.Should().Be(2);
        trip1.BookedSeats.Should().Be(2);
        trip1.OccupancyPercentage.Should().Be(100m);

        var trip2Response = await _client.GetAsync($"/api/reports/seat-occupancy?tripId={scenario.Trip2Id}");
        var trip2Body = await trip2Response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SeatOccupancyReportEntryDto>>>(TestJsonOptions.Default);
        var trip2 = trip2Body!.Data!.Single();
        trip2.TotalSeats.Should().Be(1);
        trip2.BookedSeats.Should().Be(0);
        trip2.OccupancyPercentage.Should().Be(0m);
    }

    [Fact]
    public async Task GetCustomerHistory_ReturnsThatCustomersBookingsOnly()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/customer-history?customerId={scenario.CustomerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerBookingHistoryEntryDto>>>(TestJsonOptions.Default);
        var entry = body!.Data!.Single();
        entry.BookingId.Should().Be(scenario.CancelledBookingId);
        entry.Status.Should().Be(BookingStatus.Refunded);
    }

    [Fact]
    public async Task GetPickupPointPassengers_SortedByPickupStopName()
    {
        var scenario = await SeedReportScenarioAsync();
        await AuthenticateAsStaffAsync();

        var response = await _client.GetAsync($"/api/reports/pickup-points?tripId={scenario.Trip1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PassengerReportEntryDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(2);
        body.Data!.Select(p => p.PickupStopName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetDailyBookings_AsGuest_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/reports/daily-bookings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task AuthenticateAsStaffAsync()
    {
        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
    }

    private sealed record ReportScenario(
        Guid Route1Id,
        Guid Route2Id,
        Guid Trip1Id,
        Guid Trip2Id,
        Guid CustomerId,
        Guid CancelledBookingId);

    /// <summary>
    /// Route1/Trip1 (2 seats): one Confirmed+Paid booking (fare 3500), one Pending/unpaid
    /// booking (fare 3500) — both occupy a seat, only the Confirmed one is Paid.
    /// Route2/Trip2 (1 seat): a registered customer books, pays, then the booking is cancelled
    /// by staff — ends Refunded, its payment Refunded, its seat freed again.
    /// </summary>
    private async Task<ReportScenario> SeedReportScenarioAsync()
    {
        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var (route1Id, trip1Id, trip1SeatIds, pickup1, dropOff1) = await SeedRouteAndTripAsync("A", "B", seatCount: 2);
        var (route2Id, trip2Id, trip2SeatIds, pickup2, dropOff2) = await SeedRouteAndTripAsync("C", "D", seatCount: 1);

        _client.DefaultRequestHeaders.Authorization = null;

        // Trip1: booking 1 (Confirmed + Paid).
        var lock1 = await LockSeatAsync(trip1Id, trip1SeatIds[0]);
        var booking1Id = await CreateSinglePassengerBookingAsync(trip1Id, trip1SeatIds[0], "Passenger One", pickup1, dropOff1, lock1);
        var payment1Response = await _client.PostAsJsonAsync("/api/payments", new { bookingId = booking1Id, paymentMethod = "Cash" });
        var payment1 = (await payment1Response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment1.Id}/confirm", null);

        // Trip1: booking 2 (stays Pending, never paid).
        var lock2 = await LockSeatAsync(trip1Id, trip1SeatIds[1]);
        await CreateSinglePassengerBookingAsync(trip1Id, trip1SeatIds[1], "Passenger Two", pickup1, dropOff1, lock2);

        // Trip2: registered customer books, pays, gets cancelled -> Refunded.
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Report Customer",
            email,
            phoneNumber = "+94770000000",
            password = ValidPassword
        });
        var authResult = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(TestJsonOptions.Default))!.Data!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        var lock3 = await LockSeatAsync(trip2Id, trip2SeatIds[0]);
        var booking3Id = await CreateSinglePassengerBookingAsync(trip2Id, trip2SeatIds[0], "Report Customer", pickup2, dropOff2, lock3);
        var payment3Response = await _client.PostAsJsonAsync("/api/payments", new { bookingId = booking3Id, paymentMethod = "Cash" });
        var payment3 = (await payment3Response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment3.Id}/confirm", null);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        await _client.PatchAsJsonAsync($"/api/bookings/{booking3Id}/cancel", new { cancellationReason = "Testing reports." });

        return new ReportScenario(route1Id, route2Id, trip1Id, trip2Id, authResult.User.Id, booking3Id);
    }

    private async Task<(Guid RouteId, Guid TripId, IReadOnlyList<Guid> TripSeatIds, Guid PickupStopId, Guid DropOffStopId)> SeedRouteAndTripAsync(
        string fromStop, string toStop, int seatCount)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{suffix}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;
        for (var i = 0; i < seatCount; i++)
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

        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from = fromStop, to = toStop });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;
        var pickupResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = fromStop, allowPickup = true, allowDropOff = true });
        var pickupStop = (await pickupResponse.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default))!.Data!;
        var dropOffResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = toStop, allowPickup = true, allowDropOff = true });
        var dropOffStop = (await dropOffResponse.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default))!.Data!;
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
            .Select(s => s.TripSeatId)
            .ToList();

        return (route.Id, trip.Id, tripSeatIds, pickupStop.Id, dropOffStop.Id);
    }

    private async Task<string> LockSeatAsync(Guid tripId, Guid tripSeatId)
    {
        var response = await _client.PostAsync($"/api/trips/{tripId}/seats/{tripSeatId}/lock", null);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default))!.Data!.LockId;
    }

    private async Task<Guid> CreateSinglePassengerBookingAsync(
        Guid tripId, Guid tripSeatId, string passengerName, Guid pickupStopId, Guid dropOffStopId, string lockId)
    {
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
