namespace BusBooking.Application.Common.Auditing;

/// <summary>
/// Opt-in marker for MediatR requests that AuditLoggingBehavior should record. AuditEntityId is
/// the id the request itself already knows (an Update/Cancel/Assign command's own Id) — leave
/// it null for a Create command, whose entity id doesn't exist until the handler returns; the
/// behavior falls back to reading an "Id" property off the response in that case.
/// </summary>
public interface IAuditableRequest
{
    string AuditAction { get; }

    string AuditEntityName { get; }

    Guid? AuditEntityId { get; }
}
