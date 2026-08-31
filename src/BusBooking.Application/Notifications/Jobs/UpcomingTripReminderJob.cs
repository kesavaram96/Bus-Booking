using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Notifications.Jobs;

/// <summary>
/// The one event in the doc's list that isn't triggered by a specific handler — it's
/// time-based, so it's a Hangfire *recurring* job (registered in Infrastructure's
/// DependencyInjection) rather than something enqueued from a command handler.
///
/// Deduplication is a time-window heuristic, not a strict per-booking guarantee: a recipient
/// who already got an UpcomingTripReminder in the last 20 hours is skipped. NotificationLog's
/// fields are exactly the doc's list (Recipient, type, Status, SentAt, error, retry count) —
/// no BookingId/TripId column — so "have we already reminded this exact booking" isn't
/// something a query can answer precisely; a same-day-per-recipient window is a reasonable,
/// much simpler proxy that still prevents spamming the same person every time this job runs.
/// </summary>
public sealed class UpcomingTripReminderJob
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(20);

    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;

    public UpcomingTripReminderJob(IApplicationDbContext context, IIdentityService identityService, INotificationService notificationService)
    {
        _context = context;
        _identityService = identityService;
        _notificationService = notificationService;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var horizon = now.Add(ReminderWindow);

        // DepartureDateTime is a computed property (TripDate + DepartureTime combined), not a
        // column — the Scheduled-trip candidate set is filtered in the database, the actual
        // departure-window check happens in memory over that (small) set.
        var scheduledTrips = await _context.Trips
            .Where(t => t.Status == TripStatus.Scheduled)
            .ToListAsync(cancellationToken);

        var upcomingTripIds = scheduledTrips
            .Where(t => t.DepartureDateTime > now && t.DepartureDateTime <= horizon)
            .Select(t => t.Id)
            .ToHashSet();

        if (upcomingTripIds.Count == 0)
        {
            return;
        }

        var confirmedBookings = await _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => upcomingTripIds.Contains(b.TripId) && b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var recentlyReminded = await _context.NotificationLogs
            .Where(n => n.EventType == NotificationEventType.UpcomingTripReminder && n.CreatedAt >= now.Subtract(DeduplicationWindow))
            .Select(n => n.Recipient)
            .ToListAsync(cancellationToken);
        var recentlyRemindedSet = recentlyReminded.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var booking in confirmedBookings)
        {
            var recipient = await BookingNotificationRecipientResolver.ResolveAsync(_identityService, booking, cancellationToken);
            if (recipient is null || recentlyRemindedSet.Contains(recipient.Value.Recipient))
            {
                continue;
            }

            await _notificationService.NotifyAsync(
                new NotificationRequest(
                    recipient.Value.Recipient,
                    recipient.Value.Channel,
                    NotificationEventType.UpcomingTripReminder,
                    "Your trip is coming up",
                    $"Reminder: booking {booking.BookingNumber} departs soon."),
                cancellationToken);

            recentlyRemindedSet.Add(recipient.Value.Recipient);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
