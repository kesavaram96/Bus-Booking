using BusBooking.Domain.Entities;

namespace BusBooking.Application.Payments.DTOs;

public static class PaymentMappingExtensions
{
    public static PaymentDto ToDto(this Payment payment) =>
        new(
            payment.Id,
            payment.BookingId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethod,
            payment.Status,
            payment.TransactionReference,
            payment.PaidAt,
            payment.CreatedAt);
}
