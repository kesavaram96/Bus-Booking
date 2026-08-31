using BusBooking.Application.Authentication.DTOs;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : IRequest<AuthResult>;
