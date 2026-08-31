using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Payments.Queries.GetPayments;

public sealed class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, PaginatedList<PaymentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Payments.AsNoTracking().AsQueryable();

        if (request.BookingId.HasValue)
        {
            query = query.Where(p => p.BookingId == request.BookingId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        query = request.SortDescending
            ? query.OrderByDescending(p => p.CreatedAt)
            : query.OrderBy(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var payments = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = payments.Select(p => p.ToDto()).ToList();

        return new PaginatedList<PaymentDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
