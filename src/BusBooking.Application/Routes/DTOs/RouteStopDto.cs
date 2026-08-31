namespace BusBooking.Application.Routes.DTOs;

public sealed record RouteStopDto(
    Guid Id,
    string StopName,
    int StopOrder,
    TimeSpan? ExpectedArrivalTime,
    TimeSpan? ExpectedDepartureTime,
    bool AllowPickup,
    bool AllowDropOff);
