using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.Tickets.DTOs;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.Domain.Enums;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class TicketsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetByBooking_BeforePaymentConfirmed_ReturnsEmptyList()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/tickets/booking/{booking.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TicketDto>>>(TestJsonOptions.Default);
        body!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByBooking_ForNonExistentBooking_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/tickets/booking/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByBooking_AfterPaymentConfirmed_ReturnsOneTicketPerPassengerWithAQrCode()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await CreateAndConfirmCashPaymentAsync(booking.Id);

        var response = await _client.GetAsync($"/api/tickets/booking/{booking.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TicketDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(1);

        var ticket = body.Data!.Single();
        ticket.BookingId.Should().Be(booking.Id);
        ticket.BookingNumber.Should().Be(booking.BookingNumber);
        ticket.TicketNumber.Should().StartWith("TKT");
        ticket.TicketCode.Should().NotBeNullOrWhiteSpace();
        ticket.PassengerName.Should().Be("Someone");
        ticket.SeatNumber.Should().NotBeNullOrWhiteSpace();
        ticket.PickupStopName.Should().Be("A");
        ticket.DropOffStopName.Should().Be("B");

        // A real PNG, not just an opaque non-empty string.
        var pngBytes = Convert.FromBase64String(ticket.QrCodeBase64);
        pngBytes.Should().NotBeEmpty();
        pngBytes[..8].Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);
    }

    [Fact]
    public async Task GetByBooking_CalledTwiceAfterConfirmation_StillReturnsExactlyOneTicketPerPassenger()
    {
        // ConfirmPayment's ticket generation is idempotent — this proves it doesn't matter how
        // many times the tickets endpoint (or a re-sent confirm) is hit afterward.
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await CreateAndConfirmCashPaymentAsync(booking.Id);

        await _client.GetAsync($"/api/tickets/booking/{booking.Id}");
        var response = await _client.GetAsync($"/api/tickets/booking/{booking.Id}");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TicketDto>>>(TestJsonOptions.Default);
        body!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Verify_ForValidTicket_AsStaff_ReturnsValidWithFullDetails()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await CreateAndConfirmCashPaymentAsync(booking.Id);
        var ticketsResponse = await _client.GetAsync($"/api/tickets/booking/{booking.Id}");
        var ticket = (await ticketsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TicketDto>>>(TestJsonOptions.Default))!.Data!.Single();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/tickets/verify/{ticket.TicketCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TicketVerificationDto>>(TestJsonOptions.Default);
        body!.Data!.IsValid.Should().BeTrue();
        body.Data.BookingStatus.Should().Be(BookingStatus.Confirmed);
        body.Data.PassengerName.Should().Be("Someone");
        body.Data.SeatNumber.Should().NotBeNullOrWhiteSpace();
        body.Data.PickupStopName.Should().Be("A");
        body.Data.DropOffStopName.Should().Be("B");
        body.Data.TripId.Should().Be(booking.TripId);
    }

    [Fact]
    public async Task Verify_ForUnknownCode_AsStaff_ReturnsInvalidWithoutError()
    {
        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync("/api/tickets/verify/not-a-real-ticket-code");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<TicketVerificationDto>>(TestJsonOptions.Default);
        body!.Data!.IsValid.Should().BeFalse();
        body.Data.Reason.Should().NotBeNullOrWhiteSpace();
        body.Data.PassengerName.Should().BeNull();
    }

    [Fact]
    public async Task Verify_AsGuest_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tickets/verify/anything");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task CreateAndConfirmCashPaymentAsync(Guid bookingId)
    {
        var createResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId, paymentMethod = "Cash" });
        var payment = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);
    }

    private async Task<BookingDto> SeedPendingBookingAsync()
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
        var pickupResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "A", allowPickup = true, allowDropOff = true });
        var pickupStop = (await pickupResponse.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default))!.Data!;
        var dropOffResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName = "B", allowPickup = true, allowDropOff = true });
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
        var tripSeatId = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!.Single().TripSeatId;

        var lockResponse = await _client.PostAsync($"/api/trips/{trip.Id}/seats/{tripSeatId}/lock", null);
        var lockId = (await lockResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default))!.Data!.LockId;

        _client.DefaultRequestHeaders.Authorization = null;
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
                    pickupStopId = pickupStop.Id,
                    dropOffStopId = dropOffStop.Id,
                    tripSeatId,
                    lockId
                }
            }
        });

        return (await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!;
    }
}
