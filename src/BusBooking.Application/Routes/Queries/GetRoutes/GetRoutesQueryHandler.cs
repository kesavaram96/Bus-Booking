using System.Linq.Expressions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Routes.Queries.GetRoutes;

public sealed class GetRoutesQueryHandler : IRequestHandler<GetRoutesQuery, PaginatedList<RouteSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRoutesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<RouteSummaryDto>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Routes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(r =>
                r.Name.Contains(searchTerm) || r.From.Contains(searchTerm) || r.To.Contains(searchTerm));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RouteSummaryDto(
                r.Id, r.Name, r.From, r.To, r.IsActive, r.Stops.Count, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<RouteSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<Route> ApplySorting(IQueryable<Route> query, string? sortBy, bool descending)
    {
        Expression<Func<Route, object>> keySelector = sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => r => r.Name,
            "from" => r => r.From,
            "to" => r => r.To,
            _ => r => r.CreatedAt
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
