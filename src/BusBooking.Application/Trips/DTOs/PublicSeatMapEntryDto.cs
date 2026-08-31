using BusBooking.Domain.Enums;

namespace BusBooking.Application.Trips.DTOs;

/// <summary>
/// Public seat map entry — no passenger or booking information, only what's needed to render
/// the seat and let a customer pick an available one.
/// </summary>
public sealed record PublicSeatMapEntryDto(
    Guid TripSeatId,
    string SeatNumber,
    int Row,
    int Column,
    SeatPositionType PositionType,
    TripSeatStatus Status);
