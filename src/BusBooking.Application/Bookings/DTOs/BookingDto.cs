using BusBooking.Domain.Enums;

namespace BusBooking.Application.Bookings.DTOs;

public sealed record BookingDto(
    Guid Id,
    string BookingNumber,
    Guid TripId,
    Guid? CustomerId,
    BookingStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyCollection<BookingPassengerDto> Passengers,
    string? CancellationReason,
    Guid? CancelledBy,
    DateTime? CancelledAt);
