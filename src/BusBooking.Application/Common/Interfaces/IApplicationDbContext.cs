using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// The Application layer's only EF Core dependency: exposes DbSet&lt;T&gt; per entity so feature
/// handlers can write focused, composable queries directly, rather than a repository per entity.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Bus> Buses { get; }

    DbSet<SeatLayout> SeatLayouts { get; }

    DbSet<Seat> Seats { get; }

    DbSet<Route> Routes { get; }

    DbSet<RouteStop> RouteStops { get; }

    DbSet<Driver> Drivers { get; }

    DbSet<Trip> Trips { get; }

    DbSet<Customer> Customers { get; }

    DbSet<TripSeat> TripSeats { get; }

    DbSet<Booking> Bookings { get; }

    DbSet<BookingPassenger> BookingPassengers { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Ticket> Tickets { get; }

    DbSet<NotificationLog> NotificationLogs { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
