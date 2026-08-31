namespace BusBooking.Application.Trips.DTOs;

public sealed record SeatLockDto(Guid TripSeatId, string LockId, DateTime LockedUntil);
