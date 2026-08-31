using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Notifications.Jobs;
using Hangfire;

namespace BusBooking.Infrastructure.Notifications;

public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireBackgroundJobScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void EnqueueNotificationDispatch(Guid notificationLogId) =>
        _backgroundJobClient.Enqueue<NotificationDispatchJob>(job => job.DispatchAsync(notificationLogId, CancellationToken.None));
}
