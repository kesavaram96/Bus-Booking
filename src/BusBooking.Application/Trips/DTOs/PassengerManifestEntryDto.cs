using BusBooking.Domain.Enums;

namespace BusBooking.Application.Trips.DTOs;

/// <summary>One row of a trip's printable passenger register — exactly the fields the doc
/// lists, plus the passenger's own id as a stable row identifier.</summary>
public sealed record PassengerManifestEntryDto(
    Guid BookingPassengerId,
    string SeatNumber,
    string PassengerName,
    Gender Gender,
    string PhoneNumber,
    string PickupStopName,
    string DropOffStopName,
    string BookingNumber,
    BookingStatus BookingStatus);
