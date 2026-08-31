using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.GetTrips;

public sealed class GetTripsQueryHandler : IRequestHandler<GetTripsQuery, PaginatedList<TripDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTripsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<TripDto>> Handle(GetTripsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver)
            .AsQueryable();

        if (request.RouteId.HasValue)
        {
            query = query.Where(t => t.RouteId == request.RouteId.Value);
        }

        if (request.BusId.HasValue)
        {
            query = query.Where(t => t.BusId == request.BusId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.TripDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.TripDate <= request.ToDate.Value);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var trips = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = trips.Select(t => t.ToDto()).ToList();

        return new PaginatedList<TripDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IOrderedQueryable<Trip> ApplySorting(IQueryable<Trip> query, string? sortBy, bool descending)
    {
        if (string.Equals(sortBy?.Trim(), "fare", StringComparison.OrdinalIgnoreCase))
        {
            return descending ? query.OrderByDescending(t => t.Fare) : query.OrderBy(t => t.Fare);
        }

        // Default: chronological order, which is exactly what "upcoming trips" needs.
        var ordered = descending
            ? query.OrderByDescending(t => t.TripDate)
            : query.OrderBy(t => t.TripDate);

        return descending
            ? ordered.ThenByDescending(t => t.DepartureTime)
            : ordered.ThenBy(t => t.DepartureTime);
    }
}
