namespace BusBooking.Application.Reports.DTOs;

public sealed record SeatOccupancyReportEntryDto(
    Guid TripId,
    DateOnly TripDate,
    string RouteFrom,
    string RouteTo,
    int TotalSeats,
    int BookedSeats,
    decimal OccupancyPercentage);
