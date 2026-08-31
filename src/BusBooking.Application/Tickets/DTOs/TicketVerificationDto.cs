using BusBooking.Domain.Enums;

namespace BusBooking.Application.Tickets.DTOs;

/// <summary>
/// Everything below IsValid/Reason is null when the code doesn't match any ticket at all (a
/// fake or garbled QR) — staff still get a clear "invalid" result rather than a 404, since
/// scanning a bogus code is an expected, non-exceptional outcome of verification.
/// </summary>
public sealed record TicketVerificationDto(
    bool IsValid,
    string? Reason,
    string? TicketNumber,
    string? BookingNumber,
    BookingStatus? BookingStatus,
    string? PassengerName,
    string? SeatNumber,
    Guid? TripId,
    DateOnly? TripDate,
    TimeSpan? DepartureTime,
    string? RouteFrom,
    string? RouteTo,
    string? PickupStopName,
    string? DropOffStopName);
