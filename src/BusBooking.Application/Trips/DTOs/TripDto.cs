using BusBooking.Domain.Enums;

namespace BusBooking.Application.Trips.DTOs;

/// <summary>
/// Business/admin-facing view of a trip — includes bus registration number and driver name.
/// The customer-facing trip search DTO (Phase 09) is a separate, deliberately restricted shape.
/// </summary>
public sealed record TripDto(
    Guid Id,
    Guid RouteId,
    string RouteName,
    Guid BusId,
    string BusRegistrationNumber,
    DateOnly TripDate,
    TimeSpan DepartureTime,
    TimeSpan ExpectedArrivalTime,
    Guid? DriverId,
    string? DriverName,
    decimal Fare,
    TripStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
