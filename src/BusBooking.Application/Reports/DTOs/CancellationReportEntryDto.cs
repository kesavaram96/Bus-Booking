using BusBooking.Domain.Enums;

namespace BusBooking.Application.Reports.DTOs;

public sealed record CancellationReportEntryDto(
    Guid BookingId,
    string BookingNumber,
    DateTime CancelledAt,
    string? CancellationReason,
    Guid? CancelledBy,
    BookingStatus Status,
    decimal TotalAmount,
    Guid TripId,
    DateOnly TripDate);
