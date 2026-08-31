using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Common;

/// <summary>
/// Generates a TripSeat for every active, physical seat (PositionType.Seat) in a bus's seat
/// layout — used both when a trip is first created and whenever its bus is changed. Callers
/// are responsible for calling SaveChangesAsync once, alongside whatever else they're
/// persisting, so trip + seat generation commit as a single transaction.
/// </summary>
internal static class TripSeatGeneration
{
    public static async Task GenerateForTripAsync(
        IApplicationDbContext context,
        Guid tripId,
        Guid seatLayoutId,
        CancellationToken cancellationToken)
    {
        var seatIds = await context.Seats
            .Where(s => s.SeatLayoutId == seatLayoutId && s.IsActive && s.PositionType == SeatPositionType.Seat)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var seatId in seatIds)
        {
            context.TripSeats.Add(new TripSeat(tripId, seatId));
        }
    }

    public static async Task RegenerateForTripAsync(
        IApplicationDbContext context,
        Guid tripId,
        Guid seatLayoutId,
        CancellationToken cancellationToken)
    {
        var existing = await context.TripSeats.Where(ts => ts.TripId == tripId).ToListAsync(cancellationToken);
        context.TripSeats.RemoveRange(existing);

        await GenerateForTripAsync(context, tripId, seatLayoutId, cancellationToken);
    }
}
