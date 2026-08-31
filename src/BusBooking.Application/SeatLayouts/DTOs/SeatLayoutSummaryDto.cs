namespace BusBooking.Application.SeatLayouts.DTOs;

/// <summary>
/// Lightweight shape for list/picker views (e.g. choosing a layout to assign to a bus) —
/// omits the seat collection, which can be large.
/// </summary>
public sealed record SeatLayoutSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int Rows,
    int Columns,
    int SeatCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
