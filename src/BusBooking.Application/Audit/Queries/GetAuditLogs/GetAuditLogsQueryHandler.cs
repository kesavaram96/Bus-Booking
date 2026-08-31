using BusBooking.Application.Audit.DTOs;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Audit.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(a => a.EntityName == request.EntityName);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == request.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.Timestamp >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.Timestamp < toExclusive);
        }

        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.Action, a.EntityName, a.EntityId, a.OldValues, a.NewValues, a.IPAddress, a.Timestamp))
            .ToListAsync(cancellationToken);

        return new PaginatedList<AuditLogDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
