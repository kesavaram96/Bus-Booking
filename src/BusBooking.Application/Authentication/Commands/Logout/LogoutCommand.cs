using MediatR;

namespace BusBooking.Application.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
