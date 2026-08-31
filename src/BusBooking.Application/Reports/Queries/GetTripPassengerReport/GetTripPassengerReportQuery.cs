using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetTripPassengerReport;

/// <summary>FromDate/ToDate filter on Trip.TripDate. Unlike GetPassengerManifest (Phase 16,
/// scoped to exactly one trip via its route), this spans every trip matching the filters.</summary>
public sealed record GetTripPassengerReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<PassengerReportEntryDto>>;
