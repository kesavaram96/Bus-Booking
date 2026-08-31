using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetDailyBookingReport;

public sealed class GetDailyBookingReportQueryHandler : IRequestHandler<GetDailyBookingReportQuery, IReadOnlyList<DailyBookingReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDailyBookingReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DailyBookingReportEntryDto>> Handle(
        GetDailyBookingReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Bookings.AsNoTracking().AsQueryable();

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(b => b.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(b => b.CreatedAt < toExclusive);
        }

        if (request.TripId.HasValue)
        {
            query = query.Where(b => b.TripId == request.TripId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        if (request.RouteId.HasValue)
        {
            var routeId = request.RouteId.Value;
            var tripIdsOnRoute = _context.Trips.AsNoTracking().Where(t => t.RouteId == routeId).Select(t => t.Id);
            query = query.Where(b => tripIdsOnRoute.Contains(b.TripId));
        }

        var grouped = await query
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new DailyBookingReportEntryDto(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                g.Sum(b => b.TotalAmount)))
            .ToListAsync(cancellationToken);

        return grouped.OrderBy(x => x.Date).ToList();
    }
}
