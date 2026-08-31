namespace BusBooking.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
