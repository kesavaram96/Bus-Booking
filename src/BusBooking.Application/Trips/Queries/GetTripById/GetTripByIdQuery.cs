using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Queries.GetTripById;

public sealed record GetTripByIdQuery(Guid Id) : IRequest<TripDto>;
