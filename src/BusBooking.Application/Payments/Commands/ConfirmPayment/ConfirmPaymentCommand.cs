using BusBooking.Application.Common.Auditing;
using BusBooking.Application.Payments.DTOs;
using MediatR;

namespace BusBooking.Application.Payments.Commands.ConfirmPayment;

public sealed record ConfirmPaymentCommand(Guid Id) : IRequest<PaymentDto>, IAuditableRequest
{
    public string AuditAction => "ConfirmPayment";

    public string AuditEntityName => "Payment";

    public Guid? AuditEntityId => Id;
}
