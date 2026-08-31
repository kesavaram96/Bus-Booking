using BusBooking.Domain.Entities;

namespace BusBooking.Application.Bookings.DTOs;

public static class BookingMappingExtensions
{
    /// <summary>
    /// Relies on Seat/PickupStop/DropOffStop navigations being populated — either via an
    /// explicit .Include() in a query, or via EF Core's relationship fixup when the referenced
    /// entities were loaded through the same DbContext (as CreateBookingCommandHandler does).
    /// </summary>
    public static BookingPassengerDto ToDto(this BookingPassenger passenger) =>
        new(
            passenger.Id,
            passenger.FullName,
            passenger.PhoneNumber,
            passenger.Gender,
            passenger.NIC,
            passenger.Email,
            passenger.SeatId,
            passenger.Seat.SeatNumber,
            passenger.PickupStop.StopName,
            passenger.DropOffStop.StopName,
            passenger.Fare);

    public static BookingDto ToDto(this Booking booking) =>
        new(
            booking.Id,
            booking.BookingNumber,
            booking.TripId,
            booking.CustomerId,
            booking.Status,
            booking.TotalAmount,
            booking.CreatedAt,
            booking.Passengers.Select(p => p.ToDto()).ToList(),
            booking.CancellationReason,
            booking.CancelledBy,
            booking.CancelledAt);
}
