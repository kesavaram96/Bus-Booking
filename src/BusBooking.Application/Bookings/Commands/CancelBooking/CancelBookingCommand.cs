using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Auditing;
using MediatR;

namespace BusBooking.Application.Bookings.Commands.CancelBooking;

/// <summary>CancelledBy and IsStaffCancellation must never be trusted from the request body —
/// the controller always decides both from the JWT claims, the same "must come from a trusted
/// source" rule used for Booking.Create's CustomerId.</summary>
public sealed record CancelBookingCommand(
    Guid BookingId,
    string CancellationReason,
    Guid CancelledBy,
    bool IsStaffCancellation) : IRequest<BookingDto>, IAuditableRequest
{
    public string AuditAction => "CancelBooking";

    public string AuditEntityName => "Booking";

    public Guid? AuditEntityId => BookingId;
}
