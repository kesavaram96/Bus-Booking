using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Payments.Commands.CreatePayment;

/// <summary>Amount is never taken from the client — it's always the booking's server-calculated
/// TotalAmount, the same "must come from a trusted source" rule used for Booking's fare.</summary>
public sealed record CreatePaymentCommand(Guid BookingId, PaymentMethod PaymentMethod) : IRequest<PaymentDto>, IAuditableRequest
{
    public string AuditAction => "CreatePayment";

    public string AuditEntityName => "Payment";

    public Guid? AuditEntityId => null;
}
