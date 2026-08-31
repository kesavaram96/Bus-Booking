using MediatR;

namespace BusBooking.Application.Trips.Commands.MarkDeparted;

public sealed record MarkDepartedCommand(Guid TripId) : IRequest;
