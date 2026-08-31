namespace BusBooking.Application.Routes.DTOs;

/// <summary>
/// Complete route including every stop, ordered by StopOrder.
/// </summary>
public sealed record RouteDto(
    Guid Id,
    string Name,
    string From,
    string To,
    bool IsActive,
    IReadOnlyCollection<RouteStopDto> Stops,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
