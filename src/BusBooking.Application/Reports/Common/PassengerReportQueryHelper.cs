using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Common;

/// <summary>
/// Shared by the Trip Passenger Report and the Pickup-Point Passenger Report — identical join
/// and filters, differing only in which order the caller sorts the result. One projected LINQ
/// query (Bookings → Passengers → Trip), not .Include()-then-map, so the database returns only
/// the columns this DTO actually needs.
/// </summary>
public static class PassengerReportQueryHelper
{
    public static IQueryable<PassengerReportEntryDto> BuildQuery(
        IApplicationDbContext context,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? routeId,
        Guid? tripId,
        BookingStatus? status)
    {
        var query =
            from booking in context.Bookings.AsNoTracking()
            join trip in context.Trips.AsNoTracking() on booking.TripId equals trip.Id
            from passenger in booking.Passengers
            select new { booking, trip, passenger };

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.trip.TripDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.trip.TripDate <= toDate.Value);
        }

        if (routeId.HasValue)
        {
            query = query.Where(x => x.trip.RouteId == routeId.Value);
        }

        if (tripId.HasValue)
        {
            query = query.Where(x => x.trip.Id == tripId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.booking.Status == status.Value);
        }

        return query.Select(x => new PassengerReportEntryDto(
            x.trip.Id,
            x.trip.TripDate,
            x.trip.Route.From,
            x.trip.Route.To,
            x.passenger.Seat.SeatNumber,
            x.passenger.FullName,
            x.passenger.Gender,
            x.passenger.PhoneNumber,
            x.passenger.PickupStopId,
            x.passenger.PickupStop.StopName,
            x.passenger.DropOffStop.StopName,
            x.booking.BookingNumber,
            x.booking.Status));
    }
}
