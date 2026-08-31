using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetDailyBookingReport;

/// <summary>FromDate/ToDate filter on Booking.CreatedAt (when the booking was made).</summary>
public sealed record GetDailyBookingReportQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<DailyBookingReportEntryDto>>;
