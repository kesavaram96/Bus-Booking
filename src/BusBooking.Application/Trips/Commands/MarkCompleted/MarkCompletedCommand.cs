using MediatR;

namespace BusBooking.Application.Trips.Commands.MarkCompleted;

public sealed record MarkCompletedCommand(Guid TripId) : IRequest;
