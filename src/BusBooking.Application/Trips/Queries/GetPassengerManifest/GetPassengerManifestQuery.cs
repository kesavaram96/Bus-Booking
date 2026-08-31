using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Trips.Queries.GetPassengerManifest;

/// <summary>
/// Deliberately not paginated: this backs an A4-printable register and future PDF/Excel export,
/// where "page 2 of the manifest" isn't a meaningful client request — the caller always needs
/// the whole trip's passenger list (optionally filtered/sorted) in one response.
/// </summary>
public sealed record GetPassengerManifestQuery(
    Guid TripId,
    string? SearchTerm = null,
    Guid? PickupStopId = null,
    BookingStatus? BookingStatus = null,
    bool SortDescending = false) : IRequest<IReadOnlyList<PassengerManifestEntryDto>>;
