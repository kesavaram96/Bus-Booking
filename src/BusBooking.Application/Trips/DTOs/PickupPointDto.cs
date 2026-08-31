namespace BusBooking.Application.Trips.DTOs;

public sealed record PickupPointDto(Guid RouteStopId, string StopName, TimeSpan? ExpectedDepartureTime);
