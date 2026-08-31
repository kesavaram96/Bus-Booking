using BusBooking.Domain.Entities;

namespace BusBooking.Application.SeatLayouts.DTOs;

public static class SeatLayoutMappingExtensions
{
    public static SeatDto ToDto(this Seat seat) =>
        new(seat.Id, seat.SeatNumber, seat.Row, seat.Column, seat.PositionType, seat.IsActive);

    public static SeatLayoutDto ToDto(this SeatLayout layout) =>
        new(
            layout.Id,
            layout.Name,
            layout.Description,
            layout.Rows,
            layout.Columns,
            layout.Seats.OrderBy(s => s.Row).ThenBy(s => s.Column).Select(s => s.ToDto()).ToList(),
            layout.CreatedAt,
            layout.UpdatedAt);
}
