using BusBooking.Application.Audit.DTOs;
using BusBooking.Application.Common.Models;
using MediatR;

namespace BusBooking.Application.Audit.Queries.GetAuditLogs;

/// <summary>Paginated, unlike the Phase 19 reports — an audit trail is an investigation tool
/// that only grows, not a bounded export dataset.</summary>
public sealed record GetAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? EntityName = null,
    Guid? EntityId = null,
    string? Action = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null) : IRequest<PaginatedList<AuditLogDto>>;
