using MediatR;

namespace BusBooking.Application.Trips.Commands.ScheduleTrip;

public sealed record ScheduleTripCommand(Guid TripId) : IRequest;
