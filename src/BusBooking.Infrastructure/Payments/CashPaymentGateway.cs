using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;

namespace BusBooking.Infrastructure.Payments;

/// <summary>
/// Cash isn't charged electronically — confirming a Cash payment represents staff attesting
/// they've physically collected the money, so this always succeeds immediately.
/// </summary>
public sealed class CashPaymentGateway : IPaymentGateway
{
    public bool Supports(PaymentMethod method) => method == PaymentMethod.Cash;

    public Task<PaymentGatewayResult> ChargeAsync(PaymentGatewayRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(PaymentGatewayResult.Success($"CASH-{request.PaymentId:N}"));
}
