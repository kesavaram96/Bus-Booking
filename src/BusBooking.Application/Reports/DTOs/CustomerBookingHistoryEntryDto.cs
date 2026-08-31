using BusBooking.Domain.Enums;

namespace BusBooking.Application.Reports.DTOs;

public sealed record CustomerBookingHistoryEntryDto(
    Guid BookingId,
    string BookingNumber,
    DateTime CreatedAt,
    BookingStatus Status,
    decimal TotalAmount,
    Guid TripId,
    DateOnly TripDate,
    string RouteFrom,
    string RouteTo);
