using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.AssignBus;

public sealed record AssignBusCommand(Guid TripId, Guid BusId) : IRequest<TripDto>;
