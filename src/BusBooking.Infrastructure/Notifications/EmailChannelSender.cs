using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BusBooking.Infrastructure.Notifications;

/// <summary>
/// Real MailKit-based delivery, with two modes chosen entirely by configuration: if
/// PickupDirectory is set, every message is written as a genuine .eml file there instead of
/// going out over the network — no SMTP server needed for local dev/test, the same "always
/// works, no external dependency" posture CashPaymentGateway has for payments. Otherwise it
/// connects to Host/Port for real SMTP delivery — swap that in production without any code
/// change here.
/// </summary>
public sealed class EmailChannelSender : INotificationChannelSender
{
    private readonly EmailSettings _settings;

    public EmailChannelSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public bool Supports(NotificationChannel channel) => channel == NotificationChannel.Email;

    public async Task<NotificationSendResult> SendAsync(NotificationLog notification, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(notification.Recipient));
            message.Subject = notification.Subject ?? "BusBooking notification";
            message.Body = new TextPart("plain") { Text = notification.Body };

            if (!string.IsNullOrWhiteSpace(_settings.PickupDirectory))
            {
                Directory.CreateDirectory(_settings.PickupDirectory);
                var path = Path.Combine(_settings.PickupDirectory, $"{notification.Id:N}.eml");
                await using var stream = File.Create(path);
                await message.WriteToAsync(stream, cancellationToken);
                return NotificationSendResult.Success;
            }

            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                return NotificationSendResult.Failure("No email delivery method configured (set Email:Host or Email:PickupDirectory).");
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return NotificationSendResult.Success;
        }
        catch (Exception ex)
        {
            return NotificationSendResult.Failure(ex.Message);
        }
    }
}
