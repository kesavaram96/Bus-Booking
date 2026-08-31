using BusBooking.Application.Reports.DTOs;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetSeatOccupancyReport;

/// <summary>FromDate/ToDate filter on Trip.TripDate. No Booking status filter — occupancy
/// always counts every non-cancelled booking on the trip, since that's what "occupied" means.</summary>
public sealed record GetSeatOccupancyReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId) : IRequest<IReadOnlyList<SeatOccupancyReportEntryDto>>;
