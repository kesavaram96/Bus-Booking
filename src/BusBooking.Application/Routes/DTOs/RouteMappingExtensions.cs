using BusBooking.Domain.Entities;

namespace BusBooking.Application.Routes.DTOs;

public static class RouteMappingExtensions
{
    public static RouteStopDto ToDto(this RouteStop stop) =>
        new(
            stop.Id,
            stop.StopName,
            stop.StopOrder,
            stop.ExpectedArrivalTime,
            stop.ExpectedDepartureTime,
            stop.AllowPickup,
            stop.AllowDropOff);

    public static RouteDto ToDto(this Route route) =>
        new(
            route.Id,
            route.Name,
            route.From,
            route.To,
            route.IsActive,
            route.Stops.OrderBy(s => s.StopOrder).Select(s => s.ToDto()).ToList(),
            route.CreatedAt,
            route.UpdatedAt);
}
