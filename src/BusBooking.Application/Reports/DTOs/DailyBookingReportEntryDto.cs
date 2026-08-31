namespace BusBooking.Application.Reports.DTOs;

/// <summary>One row per calendar day (grouped by Booking.CreatedAt) within the requested range.</summary>
public sealed record DailyBookingReportEntryDto(DateOnly Date, int BookingCount, decimal TotalAmount);
