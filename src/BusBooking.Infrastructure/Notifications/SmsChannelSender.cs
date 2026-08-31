using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Notifications;

/// <summary>
/// Placeholder per the doc — no real Sri Lankan SMS gateway (e.g. Dialog, Mobitel, or a
/// provider like Twilio) integrated yet. Logs intent and reports success, so the rest of the
/// pipeline (status tracking, retry counting) is fully exercised even before a real provider
/// exists — swap this for a real implementation of INotificationChannelSender when one is
/// integrated, without touching NotificationService or the dispatch job.
/// </summary>
public sealed class SmsChannelSender : INotificationChannelSender
{
    private readonly ILogger<SmsChannelSender> _logger;

    public SmsChannelSender(ILogger<SmsChannelSender> logger)
    {
        _logger = logger;
    }

    public bool Supports(NotificationChannel channel) => channel == NotificationChannel.Sms;

    public Task<NotificationSendResult> SendAsync(NotificationLog notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SMS placeholder] To {Recipient}: {Body}", notification.Recipient, notification.Body);
        return Task.FromResult(NotificationSendResult.Success);
    }
}
