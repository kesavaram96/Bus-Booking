using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>One implementation per channel (Email/Sms/WhatsApp); the dispatch job picks
/// whichever one's Supports() matches, the same registry pattern IPaymentGateway already uses
/// for Cash vs electronic payment methods.</summary>
public interface INotificationChannelSender
{
    bool Supports(NotificationChannel channel);

    Task<NotificationSendResult> SendAsync(NotificationLog notification, CancellationToken cancellationToken);
}

public sealed record NotificationSendResult(bool Succeeded, string? ErrorMessage)
{
    public static readonly NotificationSendResult Success = new(true, null);

    public static NotificationSendResult Failure(string errorMessage) => new(false, errorMessage);
}
