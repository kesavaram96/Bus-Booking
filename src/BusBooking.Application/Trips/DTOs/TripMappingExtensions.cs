using BusBooking.Domain.Entities;

namespace BusBooking.Application.Trips.DTOs;

public static class TripMappingExtensions
{
    /// <summary>
    /// Relies on Route/Bus/Driver navigations being populated — either via an explicit
    /// .Include() in a query, or via EF Core's automatic relationship fixup when the related
    /// entities were loaded through the same DbContext instance (as command handlers do).
    /// </summary>
    public static TripDto ToDto(this Trip trip) =>
        new(
            trip.Id,
            trip.RouteId,
            trip.Route.Name,
            trip.BusId,
            trip.Bus.RegistrationNumber,
            trip.TripDate,
            trip.DepartureTime,
            trip.ExpectedArrivalTime,
            trip.DriverId,
            trip.Driver?.FullName,
            trip.Fare,
            trip.Status,
            trip.CreatedAt,
            trip.UpdatedAt);
}
