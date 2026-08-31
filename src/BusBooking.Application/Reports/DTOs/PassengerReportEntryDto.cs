using BusBooking.Domain.Enums;

namespace BusBooking.Application.Reports.DTOs;

/// <summary>Shared by the Trip Passenger Report and the Pickup-Point Passenger Report — the
/// same underlying rows, just ordered differently by each report's handler.</summary>
public sealed record PassengerReportEntryDto(
    Guid TripId,
    DateOnly TripDate,
    string RouteFrom,
    string RouteTo,
    string SeatNumber,
    string PassengerName,
    Gender Gender,
    string PhoneNumber,
    Guid PickupStopId,
    string PickupStopName,
    string DropOffStopName,
    string BookingNumber,
    BookingStatus BookingStatus);
