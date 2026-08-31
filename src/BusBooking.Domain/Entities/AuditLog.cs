namespace BusBooking.Domain.Entities;

/// <summary>
/// Append-only — nothing ever updates a row once written, so there's no BaseAuditableEntity
/// UpdatedAt to track and no mutation methods here at all, only the constructor.
/// </summary>
public class AuditLog : Common.BaseEntity
{
    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = default!;

    public string EntityName { get; private set; } = default!;

    public Guid? EntityId { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public string? IPAddress { get; private set; }

    public DateTime Timestamp { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(Guid? userId, string action, string entityName, Guid? entityId, string? oldValues, string? newValues, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name is required.", nameof(entityName));
        }

        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        IPAddress = ipAddress;
        Timestamp = DateTime.UtcNow;
    }
}
