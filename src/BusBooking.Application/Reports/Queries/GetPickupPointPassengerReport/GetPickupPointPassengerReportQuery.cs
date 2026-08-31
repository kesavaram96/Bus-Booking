using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetPickupPointPassengerReport;

/// <summary>Same underlying rows as GetTripPassengerReport, ordered by pickup point instead —
/// for coordinating which stops need a bus stop-by on a given day.</summary>
public sealed record GetPickupPointPassengerReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<PassengerReportEntryDto>>;
