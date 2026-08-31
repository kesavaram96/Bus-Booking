namespace BusBooking.Application.Audit.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityName,
    Guid? EntityId,
    string? OldValues,
    string? NewValues,
    string? IPAddress,
    DateTime Timestamp);
