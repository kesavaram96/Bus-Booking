using BusBooking.Domain.Enums;

namespace BusBooking.Application.Buses.DTOs;

public sealed record BusDto(
    Guid Id,
    string RegistrationNumber,
    string? Description,
    BusType BusType,
    BusStatus Status,
    Guid? SeatLayoutId,
    string? SeatLayoutName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
