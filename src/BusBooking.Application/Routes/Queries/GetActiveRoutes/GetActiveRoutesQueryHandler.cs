using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Routes.Queries.GetActiveRoutes;

public sealed class GetActiveRoutesQueryHandler : IRequestHandler<GetActiveRoutesQuery, IReadOnlyList<RouteSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActiveRoutesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RouteSummaryDto>> Handle(GetActiveRoutesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Routes
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new RouteSummaryDto(
                r.Id, r.Name, r.From, r.To, r.IsActive, r.Stops.Count, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
