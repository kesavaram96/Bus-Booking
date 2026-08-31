using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.GetPassengerManifest;

public sealed class GetPassengerManifestQueryHandler : IRequestHandler<GetPassengerManifestQuery, IReadOnlyList<PassengerManifestEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPassengerManifestQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PassengerManifestEntryDto>> Handle(
        GetPassengerManifestQuery request,
        CancellationToken cancellationToken)
    {
        var tripExists = await _context.Trips.AnyAsync(t => t.Id == request.TripId, cancellationToken);
        if (!tripExists)
        {
            throw new NotFoundException("Trip", request.TripId);
        }

        var query = _context.Bookings
            .AsNoTracking()
            .Where(b => b.TripId == request.TripId)
            .SelectMany(b => b.Passengers, (booking, passenger) => new { booking, passenger });

        if (request.PickupStopId.HasValue)
        {
            query = query.Where(x => x.passenger.PickupStopId == request.PickupStopId.Value);
        }

        if (request.BookingStatus.HasValue)
        {
            query = query.Where(x => x.booking.Status == request.BookingStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(x =>
                x.passenger.FullName.Contains(searchTerm) ||
                x.passenger.PhoneNumber.Contains(searchTerm) ||
                x.booking.BookingNumber.Contains(searchTerm) ||
                (x.passenger.NIC != null && x.passenger.NIC.Contains(searchTerm)));
        }

        var sorted = request.SortDescending
            ? query.OrderByDescending(x => x.passenger.Seat.Row).ThenByDescending(x => x.passenger.Seat.Column)
            : query.OrderBy(x => x.passenger.Seat.Row).ThenBy(x => x.passenger.Seat.Column);

        return await sorted
            .Select(x => new PassengerManifestEntryDto(
                x.passenger.Id,
                x.passenger.Seat.SeatNumber,
                x.passenger.FullName,
                x.passenger.Gender,
                x.passenger.PhoneNumber,
                x.passenger.PickupStop.StopName,
                x.passenger.DropOffStop.StopName,
                x.booking.BookingNumber,
                x.booking.Status))
            .ToListAsync(cancellationToken);
    }
}
