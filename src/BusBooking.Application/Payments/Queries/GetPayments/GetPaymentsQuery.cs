using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Payments.Queries.GetPayments;

public sealed record GetPaymentsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? BookingId = null,
    PaymentStatus? Status = null,
    bool SortDescending = false) : IRequest<PaginatedList<PaymentDto>>;
