namespace BusBooking.Application.Trips.DTOs;

/// <summary>
/// Includes the layout's Rows/Columns alongside the seats so a client can render the grid
/// without a separate seat-layout lookup.
/// </summary>
public sealed record SeatMapDto(
    Guid TripId,
    int Rows,
    int Columns,
    IReadOnlyCollection<PublicSeatMapEntryDto> Seats);
