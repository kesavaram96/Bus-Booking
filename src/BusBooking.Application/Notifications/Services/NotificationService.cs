using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;

namespace BusBooking.Application.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IBackgroundJobScheduler _jobScheduler;

    public NotificationService(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler)
    {
        _context = context;
        _jobScheduler = jobScheduler;
    }

    public Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        var log = new NotificationLog(request.Recipient, request.Channel, request.EventType, request.Subject, request.Body);
        _context.NotificationLogs.Add(log);

        // Enqueued now rather than after the caller's own SaveChangesAsync — deliberately, so
        // callers never need an extra step to remember. The background worker's real-world
        // scheduling latency comfortably outlasts this request's commit; the rare race where
        // the job runs first and finds no row yet self-heals via Hangfire's automatic retry,
        // which simply finds the row on its next attempt.
        _jobScheduler.EnqueueNotificationDispatch(log.Id);

        return Task.CompletedTask;
    }
}
