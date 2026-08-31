using MediatR;

namespace BusBooking.Application.Trips.Commands.MarkBoarding;

public sealed record MarkBoardingCommand(Guid TripId) : IRequest;
