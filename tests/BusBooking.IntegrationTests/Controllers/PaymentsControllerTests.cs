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

public class PaymentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ForPendingBooking_Succeeds()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await CreatePaymentAsync(booking.Id, "Cash");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);
        body!.Data!.BookingId.Should().Be(booking.Id);
        body.Data.Amount.Should().Be(booking.TotalAmount);
        body.Data.Currency.Should().Be("LKR");
        body.Data.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Create_ForNonExistentBooking_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await CreatePaymentAsync(Guid.NewGuid(), "Cash");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WhenBookingAlreadyHasAPendingPayment_ReturnsBadRequest()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await CreatePaymentAsync(booking.Id, "Cash");

        var second = await CreatePaymentAsync(booking.Id, "Cash");

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WhenBookingAlreadyPaid_ReturnsBadRequest()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentId = await CreateAndGetPaymentIdAsync(booking.Id, "Cash");
        await _client.PostAsync($"/api/payments/{paymentId}/confirm", null);

        var second = await CreatePaymentAsync(booking.Id, "Cash");

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirm_CashPayment_MarksPaidAndConfirmsBooking()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentId = await CreateAndGetPaymentIdAsync(booking.Id, "Cash");

        var response = await _client.PostAsync($"/api/payments/{paymentId}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(PaymentStatus.Paid);
        body.Data.TransactionReference.Should().StartWith("CASH-");
        body.Data.PaidAt.Should().NotBeNull();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var bookingResponse = await _client.GetAsync($"/api/bookings/{booking.Id}");
        var bookingBody = await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        bookingBody!.Data!.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Theory]
    [InlineData("Card")]
    [InlineData("Online")]
    [InlineData("BankTransfer")]
    public async Task Confirm_ElectronicPayment_GoesThroughMockGatewayAndSucceeds(string paymentMethod)
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentId = await CreateAndGetPaymentIdAsync(booking.Id, paymentMethod);

        var response = await _client.PostAsync($"/api/payments/{paymentId}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(PaymentStatus.Paid);
        body.Data.TransactionReference.Should().StartWith("MOCK-");
    }

    [Fact]
    public async Task Confirm_CalledTwice_IsIdempotent()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentId = await CreateAndGetPaymentIdAsync(booking.Id, "Cash");

        var first = await _client.PostAsync($"/api/payments/{paymentId}/confirm", null);
        var firstBody = await first.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);

        var second = await _client.PostAsync($"/api/payments/{paymentId}/confirm", null);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);
        secondBody!.Data!.TransactionReference.Should().Be(firstBody!.Data!.TransactionReference);
        secondBody.Data.PaidAt.Should().Be(firstBody.Data.PaidAt);
    }

    [Fact]
    public async Task Confirm_ForNonExistentPayment_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync($"/api/payments/{Guid.NewGuid()}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayments_FilteredByBookingId_ReturnsOnlyMatching()
    {
        var booking = await SeedPendingBookingAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await CreatePaymentAsync(booking.Id, "Cash");

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/payments?bookingId={booking.Id}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<PaymentDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().OnlyContain(p => p.BookingId == booking.Id);
    }

    [Fact]
    public async Task GetPayments_AsGuest_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> CreatePaymentAsync(Guid bookingId, string paymentMethod) =>
        await _client.PostAsJsonAsync("/api/payments", new { bookingId, paymentMethod });

    private async Task<Guid> CreateAndGetPaymentIdAsync(Guid bookingId, string paymentMethod)
    {
        var response = await CreatePaymentAsync(bookingId, paymentMethod);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default);
        return body!.Data!.Id;
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
