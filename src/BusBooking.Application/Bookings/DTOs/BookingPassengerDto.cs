using BusBooking.Domain.Enums;

namespace BusBooking.Application.Bookings.DTOs;

public sealed record BookingPassengerDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    Gender Gender,
    string? NIC,
    string? Email,
    Guid SeatId,
    string SeatNumber,
    string PickupStopName,
    string DropOffStopName,
    decimal Fare);
