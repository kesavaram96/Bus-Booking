using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Queries.GetTripSeats;

public sealed record GetTripSeatsQuery(Guid TripId) : IRequest<IReadOnlyList<TripSeatDto>>;
