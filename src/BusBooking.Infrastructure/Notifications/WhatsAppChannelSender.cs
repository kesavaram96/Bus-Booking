using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Notifications;

/// <summary>Placeholder per the doc, same reasoning as SmsChannelSender — no real WhatsApp
/// Business API integration yet.</summary>
public sealed class WhatsAppChannelSender : INotificationChannelSender
{
    private readonly ILogger<WhatsAppChannelSender> _logger;

    public WhatsAppChannelSender(ILogger<WhatsAppChannelSender> logger)
    {
        _logger = logger;
    }

    public bool Supports(NotificationChannel channel) => channel == NotificationChannel.WhatsApp;

    public Task<NotificationSendResult> SendAsync(NotificationLog notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[WhatsApp placeholder] To {Recipient}: {Body}", notification.Recipient, notification.Body);
        return Task.FromResult(NotificationSendResult.Success);
    }
}
