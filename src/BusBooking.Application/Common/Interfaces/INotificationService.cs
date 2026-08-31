using BusBooking.Domain.Enums;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// The only thing the rest of the application calls to send a notification. Deliberately fast
/// and non-blocking (the doc's "do not make the booking API wait for email/SMS delivery"):
/// implementations persist a NotificationLog and hand off actual delivery to a background job,
/// never calling a channel (SMTP, SMS/WhatsApp gateway) inline.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken);
}

public sealed record NotificationRequest(string Recipient, NotificationChannel Channel, NotificationEventType EventType, string? Subject, string Body);
