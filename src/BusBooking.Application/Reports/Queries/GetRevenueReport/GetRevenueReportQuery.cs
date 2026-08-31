using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetRevenueReport;

/// <summary>FromDate/ToDate filter on Payment.PaidAt. Only Paid payments count — a refunded
/// payment (PaymentStatus.Refunded) is no longer Paid, so it naturally drops out of revenue
/// without any extra logic.</summary>
public sealed record GetRevenueReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<RevenueReportEntryDto>>;
