using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Notifications;

public class NotificationLogTests
{
    [Fact]
    public void Constructor_WithValidArgs_StartsPendingWithZeroRetries()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, "Subject", "Body");

        log.Status.Should().Be(NotificationStatus.Pending);
        log.RetryCount.Should().Be(0);
        log.SentAt.Should().BeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyRecipient_Throws()
    {
        var act = () => new NotificationLog(" ", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, "Body");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyBody_Throws()
    {
        var act = () => new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordAttempt_IncrementsRetryCount()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Sms, NotificationEventType.PaymentSuccessful, null, "Body");

        log.RecordAttempt();
        log.RecordAttempt();

        log.RetryCount.Should().Be(2);
    }

    [Fact]
    public void MarkSent_SetsStatusAndSentAtAndClearsError()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, "Body");
        log.RecordAttempt();
        log.MarkFailed("transient failure");

        log.MarkSent();

        log.Status.Should().Be(NotificationStatus.Sent);
        log.SentAt.Should().NotBeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndErrorMessage()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, "Body");

        log.MarkFailed("SMTP timed out");

        log.Status.Should().Be(NotificationStatus.Failed);
        log.ErrorMessage.Should().Be("SMTP timed out");
    }

    [Fact]
    public void MarkFailed_WithEmptyMessage_Throws()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, "Body");

        var act = () => log.MarkFailed(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkFailed_ThenRetryThenMarkSent_ReflectsARealRetrySequence()
    {
        var log = new NotificationLog("someone@example.com", NotificationChannel.Email, NotificationEventType.BookingConfirmed, null, "Body");

        log.RecordAttempt();
        log.MarkFailed("first attempt failed");
        log.RecordAttempt();
        log.MarkSent();

        log.Status.Should().Be(NotificationStatus.Sent);
        log.RetryCount.Should().Be(2);
        log.ErrorMessage.Should().BeNull();
    }
}
