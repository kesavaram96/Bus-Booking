using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

/// <summary>
/// One row per notification attempt requested via INotificationService — created (Status
/// Pending) synchronously by the API request, then updated by the background dispatch job that
/// actually calls the channel. MarkSent/MarkFailed deliberately have no status guard: a real
/// retry sequence is Pending → Failed → Failed → Sent, so "already Failed" must stay a legal
/// starting point for both methods, not just Pending.
/// </summary>
public class NotificationLog : Common.BaseAuditableEntity
{
    public string Recipient { get; private set; } = default!;

    public NotificationChannel Channel { get; private set; }

    public NotificationEventType EventType { get; private set; }

    public string? Subject { get; private set; }

    public string Body { get; private set; } = default!;

    public NotificationStatus Status { get; private set; }

    public DateTime? SentAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    public int RetryCount { get; private set; }

    private NotificationLog()
    {
    }

    public NotificationLog(string recipient, NotificationChannel channel, NotificationEventType eventType, string? subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        Recipient = recipient.Trim();
        Channel = channel;
        EventType = eventType;
        Subject = subject;
        Body = body;
        Status = NotificationStatus.Pending;
        RetryCount = 0;
    }

    /// <summary>Called once per dispatch attempt, before the channel is actually invoked.</summary>
    public void RecordAttempt() => RetryCount++;

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message is required.", nameof(errorMessage));
        }

        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
