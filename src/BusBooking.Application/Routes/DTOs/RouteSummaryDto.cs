namespace BusBooking.Application.Routes.DTOs;

/// <summary>
/// Lightweight shape for list/picker views — omits the stop collection.
/// </summary>
public sealed record RouteSummaryDto(
    Guid Id,
    string Name,
    string From,
    string To,
    bool IsActive,
    int StopCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
