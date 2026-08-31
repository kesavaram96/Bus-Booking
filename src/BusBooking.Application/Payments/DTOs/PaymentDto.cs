using BusBooking.Domain.Enums;

namespace BusBooking.Application.Payments.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    string? TransactionReference,
    DateTime? PaidAt,
    DateTime CreatedAt);
