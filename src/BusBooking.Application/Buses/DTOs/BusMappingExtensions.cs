using BusBooking.Domain.Entities;

namespace BusBooking.Application.Buses.DTOs;

public static class BusMappingExtensions
{
    public static BusDto ToDto(this Bus bus, string? seatLayoutName = null) =>
        new(
            bus.Id,
            bus.RegistrationNumber,
            bus.Description,
            bus.BusType,
            bus.Status,
            bus.SeatLayoutId,
            seatLayoutName ?? bus.SeatLayout?.Name,
            bus.CreatedAt,
            bus.UpdatedAt);
}
