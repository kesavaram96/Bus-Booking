using BusBooking.Application.Payments.DTOs;
using MediatR;

namespace BusBooking.Application.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid Id) : IRequest<PaymentDto>;
