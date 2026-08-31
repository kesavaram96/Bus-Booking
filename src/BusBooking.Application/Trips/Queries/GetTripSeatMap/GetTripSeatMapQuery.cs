using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Queries.GetTripSeatMap;

public sealed record GetTripSeatMapQuery(Guid TripId) : IRequest<SeatMapDto>;
