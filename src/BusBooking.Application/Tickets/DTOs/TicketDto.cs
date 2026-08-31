namespace BusBooking.Application.Tickets.DTOs;

/// <summary>The ticket holder's own view of their ticket — TicketCode/QrCodeBase64 are shared
/// only with whoever already holds the Booking, never exposed to a third party.</summary>
public sealed record TicketDto(
    Guid Id,
    Guid BookingId,
    string BookingNumber,
    string TicketNumber,
    string TicketCode,
    string QrCodeBase64,
    Guid TripId,
    string PassengerName,
    string SeatNumber,
    string PickupStopName,
    string DropOffStopName);
