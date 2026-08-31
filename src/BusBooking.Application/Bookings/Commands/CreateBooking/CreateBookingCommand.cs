using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Bookings.Commands.CreateBooking;

/// <summary>
/// CustomerId must never be trusted from the request body — the controller always overrides
/// it (via `command with { CustomerId = ... }`) from the JWT claim when the caller is an
/// authenticated Customer, or null otherwise, exactly like every other "must come from a
/// trusted source" field elsewhere in this codebase.
/// </summary>
public sealed record CreateBookingCommand(
    Guid TripId,
    Guid? CustomerId,
    IReadOnlyList<BookingPassengerInput> Passengers) : IRequest<BookingDto>, IAuditableRequest
{
    public string AuditAction => "CreateBooking";

    public string AuditEntityName => "Booking";

    public Guid? AuditEntityId => null;
}
