namespace BusBooking.Application.SeatLayouts.DTOs;

/// <summary>
/// Complete layout including every seat, ordered by row then column so a client can render
/// the grid directly without additional sorting.
/// </summary>
public sealed record SeatLayoutDto(
    Guid Id,
    string Name,
    string? Description,
    int Rows,
    int Columns,
    IReadOnlyCollection<SeatDto> Seats,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
