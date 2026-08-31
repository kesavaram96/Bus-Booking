using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

/// <summary>
/// Deliberately not owned by Booking (no navigation collection there, no cascade) — the doc's
/// "don't tightly couple payment to booking" instruction, so a Booking can have zero, one, or
/// several payment attempts (retries after a Failed charge) without Booking needing to know
/// anything about payment mechanics.
/// </summary>
public class Payment : Common.BaseAuditableEntity
{
    public Guid BookingId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = default!;

    public PaymentMethod PaymentMethod { get; private set; }

    public PaymentStatus Status { get; private set; }

    /// <summary>The gateway's (or, for Cash, a locally generated) reference for the settled charge. Never a card number or CVV — those are never stored here at all.</summary>
    public string? TransactionReference { get; private set; }

    public DateTime? PaidAt { get; private set; }

    private Payment()
    {
    }

    public Payment(Guid bookingId, decimal amount, string currency, PaymentMethod paymentMethod)
    {
        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException("Booking id is required.", nameof(bookingId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        BookingId = bookingId;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        Status = PaymentStatus.Pending;
    }

    /// <summary>Idempotent: confirming an already-paid payment again (a retried client call, a
    /// redelivered gateway webhook) is a safe no-op, not an error.</summary>
    public void MarkPaid(string transactionReference, DateTime paidAt)
    {
        if (Status == PaymentStatus.Paid)
        {
            return;
        }

        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending payment can be marked paid.");
        }

        if (string.IsNullOrWhiteSpace(transactionReference))
        {
            throw new ArgumentException("Transaction reference is required.", nameof(transactionReference));
        }

        Status = PaymentStatus.Paid;
        TransactionReference = transactionReference;
        PaidAt = paidAt;
    }

    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending payment can be marked failed.");
        }

        Status = PaymentStatus.Failed;
    }

    public void Cancel()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending payment can be cancelled.");
        }

        Status = PaymentStatus.Cancelled;
    }

    /// <summary>Full refund only — Phase 17 doesn't ask for partial refunds, so
    /// PartiallyRefunded stays unreached, the same way Refunded itself was pre-declared but
    /// unreached until this phase.</summary>
    public void Refund()
    {
        if (Status != PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Only a paid payment can be refunded.");
        }

        Status = PaymentStatus.Refunded;
    }
}
