using System.Linq.Expressions;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Buses.Queries.GetBuses;

public sealed class GetBusesQueryHandler : IRequestHandler<GetBusesQuery, PaginatedList<BusDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBusesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<BusDto>> Handle(GetBusesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Buses.AsNoTracking().Include(b => b.SeatLayout).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(b =>
                b.RegistrationNumber.Contains(searchTerm) ||
                (b.Description != null && b.Description.Contains(searchTerm)));
        }

        if (request.BusType.HasValue)
        {
            query = query.Where(b => b.BusType == request.BusType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var buses = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = buses.Select(b => b.ToDto()).ToList();

        return new PaginatedList<BusDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<Bus> ApplySorting(IQueryable<Bus> query, string? sortBy, bool descending)
    {
        Expression<Func<Bus, object>> keySelector = sortBy?.Trim().ToLowerInvariant() switch
        {
            "registrationnumber" => b => b.RegistrationNumber,
            "bustype" => b => b.BusType,
            "status" => b => b.Status,
            _ => b => b.CreatedAt
        };

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}
