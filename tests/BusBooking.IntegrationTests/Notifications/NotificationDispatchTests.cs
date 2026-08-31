using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Notifications.Jobs;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Xunit;

namespace BusBooking.IntegrationTests.Notifications;

/// <summary>
/// Exercises the real pipeline end to end: Hangfire.MemoryStorage actually processes the
/// enqueued job on a background thread, so these tests poll the NotificationLogs table for a
/// terminal status rather than asserting immediately — the same honest "eventually consistent
/// background job" pattern any real Hangfire-backed system needs to be tested with.
/// </summary>
public class NotificationDispatchTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationDispatchTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ConfirmPayment_DispatchesBookingConfirmedAndPaymentSuccessfulEmailsForReal()
    {
        var scenario = await SeedPendingBookingWithEmailAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId = scenario.BookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);

        var sentLogs = await WaitForSentLogsAsync(scenario.PassengerEmail, expectedCount: 2);

        sentLogs.Select(l => l.EventType).Should().BeEquivalentTo(
        [
            NotificationEventType.BookingConfirmed,
            NotificationEventType.PaymentSuccessful
        ]);
        sentLogs.Should().OnlyContain(l => l.Channel == NotificationChannel.Email);

        var emlFiles = Directory.GetFiles(_factory.EmailPickupDirectory, "*.eml");
        emlFiles.Should().HaveCountGreaterThanOrEqualTo(2);

        var messages = emlFiles.Select(f => MimeMessage.Load(f)).ToList();
        messages.Should().Contain(m => m.To.ToString().Contains(scenario.PassengerEmail) && (m.Subject ?? "").Contains("confirmed"));
        messages.Should().Contain(m => m.To.ToString().Contains(scenario.PassengerEmail) && (m.Subject ?? "").Contains("Payment"));
    }

    [Fact]
    public async Task CancelBooking_DispatchesBookingCancelledEmailForReal()
    {
        var scenario = await SeedPendingBookingWithEmailAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var staffToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.BookingStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        await _client.PatchAsJsonAsync($"/api/bookings/{scenario.BookingId}/cancel", new { cancellationReason = "Testing notifications." });

        var sentLogs = await WaitForSentLogsAsync(scenario.PassengerEmail, expectedCount: 1, eventType: NotificationEventType.BookingCancelled);

        sentLogs.Should().ContainSingle();
        sentLogs.Single().Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public async Task UpcomingTripReminderJob_ForTripDepartingSoon_NotifiesConfirmedBookingPassenger()
    {
        var scenario = await SeedPendingBookingWithEmailAsync(departsIn: TimeSpan.FromHours(6));
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId = scenario.BookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);
        await WaitForSentLogsAsync(scenario.PassengerEmail, expectedCount: 2);

        using (var scope = _factory.Services.CreateScope())
        {
            var reminderJob = scope.ServiceProvider.GetRequiredService<UpcomingTripReminderJob>();
            await reminderJob.RunAsync(CancellationToken.None);
        }

        var sentLogs = await WaitForSentLogsAsync(scenario.PassengerEmail, expectedCount: 1, eventType: NotificationEventType.UpcomingTripReminder);

        sentLogs.Should().ContainSingle();
    }

    [Fact]
    public async Task UpcomingTripReminderJob_ForTripFarInTheFuture_DoesNotNotify()
    {
        var scenario = await SeedPendingBookingWithEmailAsync(departsIn: TimeSpan.FromDays(10));
        _client.DefaultRequestHeaders.Authorization = null;
        var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new { bookingId = scenario.BookingId, paymentMethod = "Cash" });
        var payment = (await paymentResponse.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(TestJsonOptions.Default))!.Data!;
        await _client.PostAsync($"/api/payments/{payment.Id}/confirm", null);
        await WaitForSentLogsAsync(scenario.PassengerEmail, expectedCount: 2);

        using (var scope = _factory.Services.CreateScope())
        {
            var reminderJob = scope.ServiceProvider.GetRequiredService<UpcomingTripReminderJob>();
            await reminderJob.RunAsync(CancellationToken.None);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reminderLogs = await context.NotificationLogs
                .Where(l => l.Recipient == scenario.PassengerEmail && l.EventType == NotificationEventType.UpcomingTripReminder)
                .ToListAsync();

            reminderLogs.Should().BeEmpty();
        }
    }

    private async Task<IReadOnlyList<NotificationLog>> WaitForSentLogsAsync(
        string recipient, int expectedCount, NotificationEventType? eventType = null, TimeSpan? timeout = null)
    {
        // Background job delivery time is inherently variable. Diagnosed once (via the
        // exception message below, captured mid-investigation): under sustained sequential load
        // within one process, Hangfire.MemoryStorage — a lightweight community package used
        // only here as a test-only stand-in for the real Hangfire.SqlServer storage — can leave
        // a freshly Enqueue()'d job sitting unprocessed (observed as Hangfire's own monitoring
        // API reporting it "Scheduled" with zero attempts) noticeably longer than any real
        // worker-availability delay would explain. Never observed against the real production
        // storage. 90s comfortably covers even a full automatic-retry chain
        // (5s+15s+30s, this app's own configured backoff) plus that dispatch latency, without
        // masking a genuinely broken pipeline (which fails fast, in well under a second, in
        // every other run).
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(90));

        while (DateTime.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var query = context.NotificationLogs.Where(l => l.Recipient == recipient && l.Status == NotificationStatus.Sent);
            if (eventType.HasValue)
            {
                query = query.Where(l => l.EventType == eventType.Value);
            }

            var logs = await query.ToListAsync();
            if (logs.Count >= expectedCount)
            {
                return logs;
            }

            await Task.Delay(200);
        }

        using var diagScope = _factory.Services.CreateScope();
        var diagContext = diagScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var allLogs = await diagContext.NotificationLogs.Where(l => l.Recipient == recipient).ToListAsync();
        var diag = string.Join(" | ", allLogs.Select(l => $"{l.EventType}:{l.Status}:retry={l.RetryCount}:err={l.ErrorMessage}"));
        var hfMonitor = Hangfire.JobStorage.Current.GetMonitoringApi();
        var hfStats = hfMonitor.GetStatistics();
        throw new TimeoutException(
            $"Timed out waiting for {expectedCount} Sent notification(s) to {recipient}. Logs: [{diag}]. " +
            $"Hangfire: enqueued={hfStats.Enqueued} processing={hfStats.Processing} scheduled={hfStats.Scheduled} " +
            $"succeeded={hfStats.Succeeded} failed={hfStats.Failed} servers={hfStats.Servers}");
    }

    private sealed record NotificationScenario(Guid BookingId, string PassengerEmail);

    private async Task<NotificationScenario> SeedPendingBookingWithEmailAsync(TimeSpan? departsIn = null)
    {
        var adminToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var passengerEmail = $"{Guid.NewGuid():N}@example.com";

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

        var departure = DateTime.UtcNow.Add(departsIn ?? TimeSpan.FromDays(2));
        var tripResponse = await _client.PostAsJsonAsync("/api/trips", new
        {
            routeId = route.Id,
            busId = bus.Id,
            tripDate = DateOnly.FromDateTime(departure).ToString("yyyy-MM-dd"),
            departureTime = departure.ToString("HH:mm:ss"),
            expectedArrivalTime = departure.AddHours(3).ToString("HH:mm:ss"),
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
                    email = passengerEmail,
                    pickupStopId = pickupStop.Id,
                    dropOffStopId = dropOffStop.Id,
                    tripSeatId,
                    lockId
                }
            }
        });
        var bookingId = (await bookingResponse.Content.ReadFromJsonAsync<ApiResponse<BookingDto>>(TestJsonOptions.Default))!.Data!.Id;

        return new NotificationScenario(bookingId, passengerEmail);
    }
}
