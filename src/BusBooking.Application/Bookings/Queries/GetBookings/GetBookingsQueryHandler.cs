using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Bookings.Queries.GetBookings;

public sealed class GetBookingsQueryHandler : IRequestHandler<GetBookingsQuery, PaginatedList<BookingDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBookingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<BookingDto>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Passengers).ThenInclude(p => p.PickupStop)
            .Include(b => b.Passengers).ThenInclude(p => p.DropOffStop)
            .AsQueryable();

        if (request.TripId.HasValue)
        {
            query = query.Where(b => b.TripId == request.TripId.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == request.CustomerId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var bookings = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(b => b.ToDto()).ToList();

        return new PaginatedList<BookingDto>(items, totalCount, request.PageNumber, request.PageSize);
    }

    private static IOrderedQueryable<Booking> ApplySorting(IQueryable<Booking> query, string? sortBy, bool descending)
    {
        if (string.Equals(sortBy?.Trim(), "totalamount", StringComparison.OrdinalIgnoreCase))
        {
            return descending ? query.OrderByDescending(b => b.TotalAmount) : query.OrderBy(b => b.TotalAmount);
        }

        return descending ? query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt);
    }
}
