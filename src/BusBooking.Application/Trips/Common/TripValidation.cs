using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Trips.Common;

/// <summary>
/// Shared trip business-rule checks (route/bus/driver eligibility, bus double-booking) used by
/// CreateTrip, UpdateTrip and AssignBus, which all need the same validation before touching a Trip.
/// </summary>
internal static class TripValidation
{
    public static async Task<Route> LoadActiveRouteAsync(IApplicationDbContext context, Guid routeId, CancellationToken cancellationToken)
    {
        var route = await context.Routes.FirstOrDefaultAsync(r => r.Id == routeId, cancellationToken)
            ?? throw new NotFoundException("Route", routeId);

        if (!route.IsActive)
        {
            throw new ValidationException([new ValidationFailure("RouteId", "Route must be active.")]);
        }

        return route;
    }

    public static async Task<Bus> LoadAssignableBusAsync(IApplicationDbContext context, Guid busId, CancellationToken cancellationToken)
    {
        var bus = await context.Buses.FirstOrDefaultAsync(b => b.Id == busId, cancellationToken)
            ?? throw new NotFoundException("Bus", busId);

        if (bus.Status != BusStatus.Active)
        {
            throw new ValidationException([new ValidationFailure("BusId", "An inactive bus cannot be assigned to a trip.")]);
        }

        if (bus.SeatLayoutId is null)
        {
            throw new ValidationException([new ValidationFailure("BusId", "Bus must have a seat layout assigned before it can be used on a trip.")]);
        }

        return bus;
    }

    public static async Task<Driver?> LoadActiveDriverOrNullAsync(IApplicationDbContext context, Guid? driverId, CancellationToken cancellationToken)
    {
        if (driverId is null)
        {
            return null;
        }

        var driver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId.Value, cancellationToken)
            ?? throw new NotFoundException("Driver", driverId.Value);

        if (!driver.IsActive)
        {
            throw new ValidationException([new ValidationFailure("DriverId", "Driver must be active.")]);
        }

        return driver;
    }

    public static async Task EnsureBusHasNoOverlappingTripAsync(
        IApplicationDbContext context,
        Guid busId,
        DateOnly tripDate,
        TimeSpan departureTime,
        TimeSpan expectedArrivalTime,
        Guid? excludeTripId,
        CancellationToken cancellationToken)
    {
        var newStart = Trip.ComputeDepartureDateTime(tripDate, departureTime);
        var newEnd = Trip.ComputeArrivalDateTime(tripDate, departureTime, expectedArrivalTime);

        // Only trips within +/-1 day can possibly overlap, given a trip never spans more than 24h.
        var candidates = await context.Trips
            .Where(t => t.BusId == busId && t.Status != TripStatus.Cancelled)
            .Where(t => t.TripDate >= tripDate.AddDays(-1) && t.TripDate <= tripDate.AddDays(1))
            .Where(t => excludeTripId == null || t.Id != excludeTripId.Value)
            .ToListAsync(cancellationToken);

        var hasOverlap = candidates.Any(t =>
        {
            var existingStart = Trip.ComputeDepartureDateTime(t.TripDate, t.DepartureTime);
            var existingEnd = Trip.ComputeArrivalDateTime(t.TripDate, t.DepartureTime, t.ExpectedArrivalTime);

            return newStart < existingEnd && existingStart < newEnd;
        });

        if (hasOverlap)
        {
            throw new ValidationException(
            [
                new ValidationFailure("BusId", "This bus is already assigned to an overlapping trip.")
            ]);
        }
    }
}
