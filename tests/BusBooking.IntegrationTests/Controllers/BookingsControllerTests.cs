using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Authentication.DTOs;
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
using StackExchange.Redis;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.IntegrationTests.Controllers;

public class BookingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ValidPassword = "P@ssw0rd123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BookingsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsGuest_Succeeds()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var lockId = await LockAsync(scenario);
        var response = await CreateBookingAsync(scenario, [PassengerBody("Guest Passenger", lockId)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.CustomerId.Should().BeNull();
        body.Data.Status.Should().Be(BookingStatus.Pending);
        body.Data.BookingNumber.Should().StartWith("BK");
    }

    [Fact]
    public async Task Create_AsRegisteredCustomer_AutoLinksCustomerId()
    {
        var scenario = await SeedBookableTripAsync();

        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Registered Customer",
            email,
            phoneNumber = "+94770000000",
            password = ValidPassword
        });
        var authResult = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<Application.Authentication.DTOs.AuthResult>>(TestJsonOptions.Default))!.Data!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        var lockId = await LockAsync(scenario);
        var response = await CreateBookingAsync(scenario, [PassengerBody("Registered Customer", lockId)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.CustomerId.Should().Be(authResult.User.Id);
    }

    [Fact]
    public async Task Create_AttemptingToSpoofCustomerIdInBody_IsIgnored()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null; // guest — should never become linked to a random customer

        var lockId = await LockAsync(scenario);
        var response = await _client.PostAsJsonAsync("/api/bookings", new
        {
            tripId = scenario.TripId,
            customerId = Guid.NewGuid(), // attempted spoof
            passengers = new[]
            {
                new
                {
                    fullName = "Guest Passenger",
                    phoneNumber = "0771234567",
                    gender = "Male",
                    pickupStopId = scenario.PickupStopId,
                    dropOffStopId = scenario.DropOffStopId,
                    tripSeatId = scenario.TripSeatIds[0],
                    lockId
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task Create_AsBusinessStaff_ManualBooking_Succeeds()
    {
        var scenario = await SeedBookableTripAsync();

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var lockId = await LockAsync(scenario);
        var response = await CreateBookingAsync(scenario, [PassengerBody("Walk-in Passenger", lockId)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task Create_MultiplePassengersOnDifferentSeats_SumsTotalAmount()
    {
        var scenario = await SeedBookableTripAsync(seatCount: 2);
        _client.DefaultRequestHeaders.Authorization = null;

        var lockId1 = await LockAsync(scenario, scenario.TripSeatIds[0]);
        var lockId2 = await LockAsync(scenario, scenario.TripSeatIds[1]);

        var response = await CreateBookingAsync(scenario,
        [
            PassengerBody("Passenger One", lockId1, scenario.TripSeatIds[0]),
            PassengerBody("Passenger Two", lockId2, scenario.TripSeatIds[1])
        ]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.Passengers.Should().HaveCount(2);
        body.Data.TotalAmount.Should().Be(scenario.Fare * 2);
        body.Data.Passengers.Should().OnlyContain(p => p.Fare == scenario.Fare);
    }

    [Fact]
    public async Task Create_OnDraftTrip_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync(schedule: false);
        _client.DefaultRequestHeaders.Authorization = null;

        // Can't lock a seat on an unscheduled trip through the normal flow either, but the
        // lock endpoint itself doesn't check trip status — only booking does — so this still
        // exercises the trip-bookability check specifically.
        var lockId = await LockAsync(scenario);
        var response = await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithPickupStopNotOnRoute_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);

        var response = await _client.PostAsJsonAsync("/api/bookings", new
        {
            tripId = scenario.TripId,
            passengers = new[]
            {
                new
                {
                    fullName = "Someone",
                    phoneNumber = "0771234567",
                    gender = "Male",
                    pickupStopId = Guid.NewGuid(), // not on this route
                    dropOffStopId = scenario.DropOffStopId,
                    tripSeatId = scenario.TripSeatIds[0],
                    lockId
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithPickupAfterDropOff_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);

        var response = await _client.PostAsJsonAsync("/api/bookings", new
        {
            tripId = scenario.TripId,
            passengers = new[]
            {
                new
                {
                    fullName = "Someone",
                    phoneNumber = "0771234567",
                    gender = "Male",
                    pickupStopId = scenario.DropOffStopId, // swapped
                    dropOffStopId = scenario.PickupStopId,
                    tripSeatId = scenario.TripSeatIds[0],
                    lockId
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutLockingSeatFirst_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        // Never locked — Status is still Available.
        var response = await CreateBookingAsync(scenario, [PassengerBody("Someone", "fabricated-lock-id")]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithWrongLockId_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        await LockAsync(scenario);

        var response = await CreateBookingAsync(scenario, [PassengerBody("Someone", "not-the-real-token")]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithLockIdFromBeforeAnExpiredLockWasReacquired_ReturnsBadRequest()
    {
        // The doc's "booking after lock expiration" concurrency scenario: a lock's Redis key
        // can expire (or be deleted) without anyone explicitly invalidating the token the
        // original caller is holding — only once someone else actually re-locks the seat does
        // the database's record of "who holds it" change, and that's the point at which the
        // original, now-stale token must stop working.
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var staleLockId = await LockAsync(scenario);

        // Simulate the original 10-minute TTL having elapsed — deleting the key is exactly
        // what Redis itself does on expiry (same technique the Phase 11 expiry test uses).
        var multiplexer = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        await multiplexer.GetDatabase().KeyDeleteAsync($"BusBooking:Dev:seatlock:{scenario.TripSeatIds[0]}");

        // A second customer re-locks the now-free seat, overwriting the database's record of
        // who currently holds it.
        var freshLockId = await LockAsync(scenario);
        freshLockId.Should().NotBe(staleLockId);

        var response = await CreateBookingAsync(scenario, [PassengerBody("Late Customer", staleLockId)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_TwoCustomersRacingForTheSameSeat_ExactlyOneBookingSucceeds()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        // Created up front and kept alive through WhenAll, disposed only afterward — disposing
        // inside the Select lambda was a real bug this suite already hit once (Phase 11).
        var clients = Enumerable.Range(0, 2).Select(_ => _factory.CreateClient()).ToList();
        try
        {
            var results = await Task.WhenAll(clients.Select(async client =>
            {
                var lockResponse = await client.PostAsync(
                    $"/api/trips/{scenario.TripId}/seats/{scenario.TripSeatIds[0]}/lock", null);

                if (!lockResponse.IsSuccessStatusCode)
                {
                    return lockResponse.StatusCode;
                }

                var lockBody = await lockResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default);

                var bookingResponse = await client.PostAsJsonAsync("/api/bookings", new
                {
                    tripId = scenario.TripId,
                    passengers = new[]
                    {
                        new
                        {
                            fullName = "Racing Customer",
                            phoneNumber = "0771234567",
                            gender = "Male",
                            pickupStopId = scenario.PickupStopId,
                            dropOffStopId = scenario.DropOffStopId,
                            tripSeatId = scenario.TripSeatIds[0],
                            lockId = lockBody!.Data!.LockId
                        }
                    }
                });

                return bookingResponse.StatusCode;
            }));

            results.Should().ContainSingle(status => status == HttpStatusCode.Created);
            results.Should().Contain(status => status != HttpStatusCode.Created);
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
    public async Task Create_WithDuplicateSeatAcrossPassengers_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);

        var response = await CreateBookingAsync(scenario,
        [
            PassengerBody("Passenger One", lockId),
            PassengerBody("Passenger Two", lockId)
        ]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AfterBooking_SeatRevertsToAvailableRatherThanStayingBooked()
    {
        // Phase 13: a seat has no global "Booked" state — after a successful booking it reverts
        // to Available so it can still be locked and booked for a different, non-overlapping
        // journey segment. Whether it's actually free for a given segment is decided at booking
        // time against existing bookings, not by this status.
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);

        var response = await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var seatsResponse = await _client.GetAsync($"/api/trips/{scenario.TripId}/seats");
        var seats = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!;

        var bookedSeat = seats.Single(s => s.TripSeatId == scenario.TripSeatIds[0]);
        bookedSeat.Status.Should().Be(TripSeatStatus.Available);
    }

    [Fact]
    public async Task Create_ForNonOverlappingSegmentOnSameSeat_Succeeds()
    {
        // Doc's worked example: Colombo -> Kurunegala booked on seat 12; Kurunegala -> Jaffna on
        // the same seat must still be allowed.
        var scenario = await SeedBookableTripAsync(stopNames: ["Colombo", "Kurunegala", "Dambulla", "Jaffna"]);
        _client.DefaultRequestHeaders.Authorization = null;

        var firstLockId = await LockAsync(scenario);
        var first = await CreateBookingAsync(scenario,
            [PassengerBody("First", firstLockId, pickupStopId: scenario.StopIds[0], dropOffStopId: scenario.StopIds[1])]);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondLockId = await LockAsync(scenario);
        var second = await CreateBookingAsync(scenario,
            [PassengerBody("Second", secondLockId, pickupStopId: scenario.StopIds[1], dropOffStopId: scenario.StopIds[3])]);

        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ForSegmentOverlappingExistingBooking_ReturnsBadRequest()
    {
        // Existing: Kurunegala -> Jaffna. New: Dambulla -> Jaffna overlaps it and must be
        // rejected, even though it doesn't start at the same stop as the existing booking.
        var scenario = await SeedBookableTripAsync(stopNames: ["Colombo", "Kurunegala", "Dambulla", "Jaffna"]);
        _client.DefaultRequestHeaders.Authorization = null;

        var firstLockId = await LockAsync(scenario);
        var first = await CreateBookingAsync(scenario,
            [PassengerBody("First", firstLockId, pickupStopId: scenario.StopIds[1], dropOffStopId: scenario.StopIds[3])]);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondLockId = await LockAsync(scenario);
        var second = await CreateBookingAsync(scenario,
            [PassengerBody("Second", secondLockId, pickupStopId: scenario.StopIds[2], dropOffStopId: scenario.StopIds[3])]);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ReleasesTheUnderlyingRedisLockAfterBooking()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);

        await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);

        var multiplexer = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var exists = await multiplexer.GetDatabase().KeyExistsAsync($"BusBooking:Dev:seatlock:{scenario.TripSeatIds[0]}");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Create_SecondAttemptOnSameSeatSameSegmentAfterFirstBooking_ReturnsBadRequest()
    {
        // The seat itself reverts to Available after booking (Phase 13), so re-locking it
        // succeeds — "prevent duplicate booking" is now enforced by the segment-overlap check
        // at booking time, not by the seat's lock status.
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var firstLockId = await LockAsync(scenario);

        var first = await CreateBookingAsync(scenario, [PassengerBody("First", firstLockId)]);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondLockId = await LockAsync(scenario);
        var second = await CreateBookingAsync(scenario, [PassengerBody("Second", secondLockId)]);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBookingById_AsStaff_ReturnsFullDetails()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var createResponse = await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);
        var bookingId = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!.Id;

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/bookings/{bookingId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.Passengers.Single().SeatNumber.Should().NotBeNullOrWhiteSpace();
        body.Data.Passengers.Single().PickupStopName.Should().Be("A");
    }

    [Fact]
    public async Task GetBookingById_AsGuest_ReturnsUnauthorized()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/bookings/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBookings_FilteredByTripId_ReturnsOnlyMatching()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.GetAsync($"/api/bookings?tripId={scenario.TripId}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<BookingDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().OnlyContain(b => b.TripId == scenario.TripId);
    }

    [Fact]
    public async Task Cancel_ByStaff_ForPendingBooking_Succeeds()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Customer called in." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(BookingStatus.Cancelled);
        body.Data.CancellationReason.Should().Be("Customer called in.");
        body.Data.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_ByStaff_ForConfirmedPaidBooking_RefundsPaymentAndMarksBookingRefunded()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Bus broke down." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(BookingStatus.Refunded);

        var paymentsResponse = await _client.GetAsync($"/api/payments?bookingId={bookingId}");
        var payments = (await paymentsResponse.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<PaymentDto>>>(TestJsonOptions.Default))!.Data!;
        payments.Items.Single().Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task Cancel_ByOwningCustomer_Succeeds()
    {
        var scenario = await SeedBookableTripAsync();
        var (accessToken, _) = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Change of plans." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ByNonOwningCustomer_ReturnsForbidden()
    {
        var scenario = await SeedBookableTripAsync();
        var (ownerToken, _) = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var (otherToken, _) = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Not mine." });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_AsGuest_ReturnsUnauthorized()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Anything." });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cancel_WhenAlreadyCancelled_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "First cancellation." });

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Second attempt." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_ForCompletedTrip_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync();
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var opsToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.OperationsManager);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);
        await _client.PatchAsync($"/api/trips/{scenario.TripId}/boarding", null);
        await _client.PatchAsync($"/api/trips/{scenario.TripId}/departed", null);
        await _client.PatchAsync($"/api/trips/{scenario.TripId}/completed", null);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Too late." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_ByCustomer_WithinCancellationWindow_ReturnsBadRequest()
    {
        var scenario = await SeedBookableTripAsync(departsIn: TimeSpan.FromMinutes(30));
        var (accessToken, _) = await RegisterCustomerAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Too close to departure." });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_ByStaff_WithinCancellationWindow_StillSucceeds()
    {
        // Staff bypass the customer-facing cancellation window entirely.
        var scenario = await SeedBookableTripAsync(departsIn: TimeSpan.FromMinutes(30));
        _client.DefaultRequestHeaders.Authorization = null;
        var lockId = await LockAsync(scenario);
        var bookingId = await CreateAndGetBookingIdAsync(scenario, lockId);

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var response = await _client.PatchAsJsonAsync($"/api/bookings/{bookingId}/cancel", new { cancellationReason = "Ops override." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateAndGetBookingIdAsync(BookableTripScenario scenario, string lockId)
    {
        var response = await CreateBookingAsync(scenario, [PassengerBody("Someone", lockId)]);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!.Id;
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterCustomerAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Registered Customer",
            email,
            phoneNumber = "+94770000000",
            password = ValidPassword
        });
        var authResult = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(TestJsonOptions.Default))!.Data!;
        return (authResult.AccessToken, authResult.User.Id);
    }

    private sealed record TestPassengerInput(
        string FullName, string LockId, Guid? TripSeatId = null, Guid? PickupStopId = null, Guid? DropOffStopId = null);

    private static TestPassengerInput PassengerBody(
        string fullName, string lockId, Guid? tripSeatId = null, Guid? pickupStopId = null, Guid? dropOffStopId = null) =>
        new(fullName, lockId, tripSeatId, pickupStopId, dropOffStopId);

    private Task<HttpResponseMessage> CreateBookingAsync(BookableTripScenario scenario, TestPassengerInput[] passengerInputs)
    {
        var passengers = passengerInputs.Select(p => new
        {
            fullName = p.FullName,
            phoneNumber = "0771234567",
            gender = "Male",
            pickupStopId = p.PickupStopId ?? scenario.PickupStopId,
            dropOffStopId = p.DropOffStopId ?? scenario.DropOffStopId,
            tripSeatId = p.TripSeatId ?? scenario.TripSeatIds[0],
            lockId = p.LockId
        });

        return _client.PostAsJsonAsync("/api/bookings", new { tripId = scenario.TripId, passengers });
    }

    private Task<string> LockAsync(BookableTripScenario scenario, Guid? tripSeatId = null) =>
        LockRawAsync(scenario.TripId, tripSeatId ?? scenario.TripSeatIds[0]);

    private async Task<string> LockRawAsync(Guid tripId, Guid tripSeatId)
    {
        var response = await _client.PostAsync($"/api/trips/{tripId}/seats/{tripSeatId}/lock", null);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLockDto>>(TestJsonOptions.Default);
        return body!.Data!.LockId;
    }

    private sealed record BookableTripScenario(
        Guid TripId,
        Guid PickupStopId,
        Guid DropOffStopId,
        IReadOnlyList<Guid> StopIds,
        IReadOnlyList<Guid> TripSeatIds,
        decimal Fare);

    private async Task<BookableTripScenario> SeedBookableTripAsync(
        int seatCount = 1, bool schedule = true, string[]? stopNames = null, TimeSpan? departsIn = null)
    {
        stopNames ??= ["A", "B"];
        var departure = DateTime.UtcNow.Add(departsIn ?? TimeSpan.FromDays(2));
        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var layoutResponse = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = $"L-{suffix}", rows = 10, columns = 4 });
        var layout = (await layoutResponse.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default))!.Data!;

        for (var i = 0; i < seatCount; i++)
        {
            await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new
            {
                seatNumber = $"{i:00}",
                row = 0,
                column = i,
                positionType = "Seat"
            });
        }

        var busResponse = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });
        var bus = (await busResponse.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PatchAsJsonAsync($"/api/buses/{bus.Id}/seat-layout", new { seatLayoutId = layout.Id });

        var routeResponse = await _client.PostAsJsonAsync("/api/routes", new { name = $"R-{suffix}", from = stopNames[0], to = stopNames[^1] });
        var route = (await routeResponse.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default))!.Data!;

        var stopIds = new List<Guid>();
        foreach (var stopName in stopNames)
        {
            var stopResponse = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new { stopName, allowPickup = true, allowDropOff = true });
            var stop = (await stopResponse.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default))!.Data!;
            stopIds.Add(stop.Id);
        }

        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        const decimal fare = 3500m;
        var tripResponse = await _client.PostAsJsonAsync("/api/trips", new
        {
            routeId = route.Id,
            busId = bus.Id,
            tripDate = DateOnly.FromDateTime(departure).ToString("yyyy-MM-dd"),
            departureTime = departure.ToString("HH:mm:ss"),
            expectedArrivalTime = departure.AddHours(3).ToString("HH:mm:ss"),
            fare
        });
        var trip = (await tripResponse.Content.ReadFromJsonAsync<ApiResponse<TripDto>>(TestJsonOptions.Default))!.Data!;

        if (schedule)
        {
            await _client.PatchAsync($"/api/trips/{trip.Id}/schedule", null);
        }

        var seatsResponse = await _client.GetAsync($"/api/trips/{trip.Id}/seats");
        var seats = (await seatsResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TripSeatDto>>>(TestJsonOptions.Default))!.Data!;

        return new BookableTripScenario(trip.Id, stopIds[0], stopIds[^1], stopIds, seats.Select(s => s.TripSeatId).ToList(), fare);
    }
}
