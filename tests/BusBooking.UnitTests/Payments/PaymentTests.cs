using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BusBooking.UnitTests.Payments;

public class PaymentTests
{
    [Fact]
    public void Constructor_WithValidArgs_StartsPending()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Amount.Should().Be(3500m);
        payment.Currency.Should().Be("LKR");
        payment.PaymentMethod.Should().Be(PaymentMethod.Cash);
        payment.TransactionReference.Should().BeNull();
        payment.PaidAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyBookingId_Throws()
    {
        var act = () => new Payment(Guid.Empty, 3500m, "LKR", PaymentMethod.Cash);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAmount_Throws(decimal amount)
    {
        var act = () => new Payment(Guid.NewGuid(), amount, "LKR", PaymentMethod.Cash);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_Throws()
    {
        var act = () => new Payment(Guid.NewGuid(), 3500m, " ", PaymentMethod.Cash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkPaid_FromPending_TransitionsToPaid()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        var paidAt = DateTime.UtcNow;

        payment.MarkPaid("CASH-123", paidAt);

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.TransactionReference.Should().Be("CASH-123");
        payment.PaidAt.Should().Be(paidAt);
    }

    [Fact]
    public void MarkPaid_CalledTwiceWithSameOutcome_IsIdempotent()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        var paidAt = DateTime.UtcNow;
        payment.MarkPaid("CASH-123", paidAt);

        var act = () => payment.MarkPaid("CASH-123", DateTime.UtcNow.AddMinutes(1));

        act.Should().NotThrow();
        payment.TransactionReference.Should().Be("CASH-123");
        payment.PaidAt.Should().Be(paidAt);
    }

    [Fact]
    public void MarkPaid_WithEmptyTransactionReference_Throws()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);

        var act = () => payment.MarkPaid(" ", DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkPaid_WhenFailed_Throws()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        payment.MarkFailed();

        var act = () => payment.MarkPaid("CASH-123", DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_FromPending_TransitionsToFailed()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Card);

        payment.MarkFailed();

        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkFailed_WhenAlreadyPaid_Throws()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        payment.MarkPaid("CASH-123", DateTime.UtcNow);

        var act = () => payment.MarkFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPending_TransitionsToCancelled()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Online);

        payment.Cancel();

        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyPaid_Throws()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        payment.MarkPaid("CASH-123", DateTime.UtcNow);

        var act = () => payment.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Refund_FromPaid_TransitionsToRefunded()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);
        payment.MarkPaid("CASH-123", DateTime.UtcNow);

        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void Refund_WhenNotPaid_Throws()
    {
        var payment = new Payment(Guid.NewGuid(), 3500m, "LKR", PaymentMethod.Cash);

        var act = () => payment.Refund();

        act.Should().Throw<InvalidOperationException>();
    }
}
