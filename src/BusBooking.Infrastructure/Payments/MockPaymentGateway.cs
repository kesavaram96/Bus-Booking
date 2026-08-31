using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;

namespace BusBooking.Infrastructure.Payments;

/// <summary>
/// Stands in for a real Sri Lankan electronic payment provider (Card/Online/BankTransfer)
/// until one is integrated — always succeeds, with a fake transaction reference. Swapping it
/// for a real provider means registering a different <see cref="IPaymentGateway"/> in
/// DependencyInjection; nothing in the Booking/Payment domain needs to change.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public bool Supports(PaymentMethod method) => method != PaymentMethod.Cash;

    public Task<PaymentGatewayResult> ChargeAsync(PaymentGatewayRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(PaymentGatewayResult.Success($"MOCK-{Guid.NewGuid():N}"));
}
