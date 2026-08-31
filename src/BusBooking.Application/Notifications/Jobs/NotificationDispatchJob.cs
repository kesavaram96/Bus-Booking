using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Notifications.Jobs;

/// <summary>
/// What Hangfire actually invokes for every enqueued notification. Lives in Application, not
/// Infrastructure, since its own logic — load the log, pick a channel, record the outcome — is
/// pure orchestration over Application-level abstractions (IApplicationDbContext,
/// INotificationChannelSender), exactly like a MediatR handler; Hangfire's job activator
/// resolves it from the app's DI container regardless of which layer it lives in.
/// </summary>
public sealed class NotificationDispatchJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<INotificationChannelSender> _senders;

    public NotificationDispatchJob(IApplicationDbContext context, IEnumerable<INotificationChannelSender> senders)
    {
        _context = context;
        _senders = senders;
    }

    public async Task DispatchAsync(Guid notificationLogId, CancellationToken cancellationToken)
    {
        var log = await _context.NotificationLogs.FirstOrDefaultAsync(n => n.Id == notificationLogId, cancellationToken);
        if (log is null)
        {
            // Most likely the originating request's transaction hasn't committed yet (this job
            // was enqueued before that SaveChangesAsync ran) — throwing lets Hangfire's own
            // automatic retry try again shortly, by which point the row will exist.
            throw new InvalidOperationException($"NotificationLog {notificationLogId} not found.");
        }

        if (log.Status == NotificationStatus.Sent)
        {
            return;
        }

        log.RecordAttempt();

        var sender = _senders.FirstOrDefault(s => s.Supports(log.Channel));
        var result = sender is null
            ? NotificationSendResult.Failure($"No sender registered for channel {log.Channel}.")
            : await sender.SendAsync(log, cancellationToken);

        if (result.Succeeded)
        {
            log.MarkSent();
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        log.MarkFailed(result.ErrorMessage ?? "Unknown error.");
        await _context.SaveChangesAsync(cancellationToken);

        // Throwing (rather than swallowing) lets Hangfire's own automatic retry re-invoke this
        // job with backoff, so RetryCount keeps growing until it either succeeds or Hangfire's
        // attempt limit is exhausted — a real, meaningful record of what actually happened,
        // not a field that's always 1.
        throw new InvalidOperationException($"Notification delivery failed: {log.ErrorMessage}");
    }
}
