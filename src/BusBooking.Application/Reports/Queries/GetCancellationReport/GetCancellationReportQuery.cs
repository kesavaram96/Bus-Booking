using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetCancellationReport;

/// <summary>FromDate/ToDate filter on Booking.CancelledAt. Base set is every booking that was
/// ever cancelled (Cancelled or Refunded); Status further narrows to exactly one of those two
/// if given.</summary>
public sealed record GetCancellationReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<CancellationReportEntryDto>>;
