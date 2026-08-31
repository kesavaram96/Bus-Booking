namespace BusBooking.Application.Trips.DTOs;

/// <summary>
/// Customer-facing search result. Deliberately excludes bus registration number, internal
/// bus id and driver information — TripDto (the business/admin shape) is a separate type
/// specifically so this restriction can never accidentally regress.
/// </summary>
public sealed record TripSearchResultDto(
    Guid Id,
    string From,
    string To,
    DateOnly TripDate,
    TimeSpan DepartureTime,
    TimeSpan ExpectedArrivalTime,
    int AvailableSeatCount,
    decimal Fare,
    IReadOnlyCollection<PickupPointDto> PickupPoints);
