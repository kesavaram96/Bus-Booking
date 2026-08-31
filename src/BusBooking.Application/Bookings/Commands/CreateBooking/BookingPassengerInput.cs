using BusBooking.Domain.Enums;

namespace BusBooking.Application.Bookings.Commands.CreateBooking;

/// <summary>
/// TripSeatId (not SeatId) because that's what the client actually has after locking a seat
/// (Phase 11's LockSeat returns TripSeatId + LockId) — the handler resolves the underlying
/// physical Seat.Id from the TripSeat, which is what BookingPassenger ultimately stores.
/// </summary>
public sealed record BookingPassengerInput(
    string FullName,
    string PhoneNumber,
    Gender Gender,
    string? NIC,
    string? Email,
    Guid PickupStopId,
    Guid DropOffStopId,
    Guid TripSeatId,
    string LockId);
