using BusBooking.Domain.Enums;

namespace BusBooking.Application.SeatLayouts.DTOs;

public sealed record SeatDto(
    Guid Id,
    string SeatNumber,
    int Row,
    int Column,
    SeatPositionType PositionType,
    bool IsActive);
