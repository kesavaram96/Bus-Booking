using System.Linq.Expressions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.SeatLayouts.Queries.GetSeatLayouts;

public sealed class GetSeatLayoutsQueryHandler : IRequestHandler<GetSeatLayoutsQuery, PaginatedList<SeatLayoutSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSeatLayoutsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<SeatLayoutSummaryDto>> Handle(
        GetSeatLayoutsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.SeatLayouts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(sl => sl.Name.Contains(searchTerm));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(sl => new SeatLayoutSummaryDto(
                sl.Id,
                sl.Name,
                sl.Description,
                sl.Rows,
                sl.Columns,
                sl.Seats.Count,
                sl.CreatedAt,
                sl.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<SeatLayoutSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<SeatLayout> ApplySorting(IQueryable<SeatLayout> query, string? sortBy, bool descending)
    {
        Expression<Func<SeatLayout, object>> keySelector = sortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => sl => sl.Name,
            _ => sl => sl.CreatedAt
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
