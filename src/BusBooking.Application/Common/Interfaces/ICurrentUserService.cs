namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// The one deliberate exception to this codebase's usual rule ("who's acting" is an explicit
/// command parameter decided by the controller from JWT claims, e.g. Booking.Create's
/// CustomerId) — audit logging is a cross-cutting concern applied uniformly by a pipeline
/// behavior to many unrelated commands, so it needs ambient access to the caller's identity and
/// IP rather than every audited command carrying its own copy of both.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? IpAddress { get; }
}
