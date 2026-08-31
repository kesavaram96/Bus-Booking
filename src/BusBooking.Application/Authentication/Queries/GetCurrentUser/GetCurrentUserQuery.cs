using BusBooking.Application.Authentication.DTOs;
using MediatR;

namespace BusBooking.Application.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;
