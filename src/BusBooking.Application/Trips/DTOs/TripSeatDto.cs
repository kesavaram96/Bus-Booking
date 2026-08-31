using BusBooking.Domain.Enums;

namespace BusBooking.Application.Trips.DTOs;

/// <summary>
/// Business-facing seat view. Status reflects only Available/Held/Blocked — since Phase 13 a
/// seat can be booked for several non-overlapping journey segments at once, so there is no
/// single "booked" state or booking id to report here; staff who need to see which booking(s)
/// occupy a seat use GetBookings?tripId= instead.
/// </summary>
public sealed record TripSeatDto(
    Guid TripSeatId,
    Guid SeatId,
    string SeatNumber,
    int Row,
    int Column,
    SeatPositionType PositionType,
    TripSeatStatus Status);
