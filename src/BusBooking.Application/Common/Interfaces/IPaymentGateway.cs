using BusBooking.Domain.Enums;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// Lets a real Sri Lankan payment provider be plugged in later without the Booking/Payment
/// domain ever changing. Several implementations may be registered (Cash, a mock for now, a
/// real provider later); a caller picks the one whose <see cref="Supports"/> matches the
/// payment's method.
/// </summary>
public interface IPaymentGateway
{
    bool Supports(PaymentMethod method);

    Task<PaymentGatewayResult> ChargeAsync(PaymentGatewayRequest request, CancellationToken cancellationToken);
}

/// <summary>Deliberately carries no card number or CVV — those must never reach this codebase at all.</summary>
public sealed record PaymentGatewayRequest(Guid PaymentId, decimal Amount, string Currency, PaymentMethod Method);

public sealed record PaymentGatewayResult(bool Succeeded, string? TransactionReference, string? FailureReason)
{
    public static PaymentGatewayResult Success(string transactionReference) => new(true, transactionReference, null);

    public static PaymentGatewayResult Failure(string reason) => new(false, null, reason);
}
