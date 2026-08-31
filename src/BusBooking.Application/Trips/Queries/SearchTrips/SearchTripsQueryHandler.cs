using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.SearchTrips;

public sealed class SearchTripsQueryHandler : IRequestHandler<SearchTripsQuery, PaginatedList<TripSearchResultDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchTripsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<TripSearchResultDto>> Handle(SearchTripsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.Trim();
        var to = request.To.Trim();

        // Available seat count is computed from the bus's active physical seats. There is no
        // TripSeat/Booking yet (Phases 10/12), so every seat genuinely is unbooked right now —
        // this is a correct interim answer, to be replaced once per-trip seat state exists.
        var query = _context.Trips
            .AsNoTracking()
            .Where(t => t.Status == TripStatus.Scheduled)
            .Where(t => t.TripDate == request.Date)
            .Where(t => t.Route.From == from && t.Route.To == to)
            .Select(t => new
            {
                t.Id,
                t.RouteId,
                t.TripDate,
                t.DepartureTime,
                t.ExpectedArrivalTime,
                t.Fare,
                AvailableSeatCount = t.Bus.SeatLayout == null
                    ? 0
                    : t.Bus.SeatLayout.Seats.Count(s => s.IsActive && s.PositionType == SeatPositionType.Seat)
            })
            .Where(t => t.AvailableSeatCount > 0);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = await query
            .OrderBy(t => t.DepartureTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var routeIds = page.Select(t => t.RouteId).Distinct().ToList();

        // One batched query for pickup points across the whole page, instead of one per trip.
        var pickupPoints = await _context.RouteStops
            .AsNoTracking()
            .Where(rs => routeIds.Contains(rs.RouteId) && rs.AllowPickup)
            .OrderBy(rs => rs.StopOrder)
            .Select(rs => new { rs.RouteId, Point = new PickupPointDto(rs.Id, rs.StopName, rs.ExpectedDepartureTime) })
            .ToListAsync(cancellationToken);

        var pickupPointsByRoute = pickupPoints
            .GroupBy(x => x.RouteId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<PickupPointDto>)g.Select(x => x.Point).ToList());

        var items = page
            .Select(t => new TripSearchResultDto(
                t.Id,
                from,
                to,
                t.TripDate,
                t.DepartureTime,
                t.ExpectedArrivalTime,
                t.AvailableSeatCount,
                t.Fare,
                pickupPointsByRoute.TryGetValue(t.RouteId, out var points) ? points : []))
            .ToList();

        return new PaginatedList<TripSearchResultDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
