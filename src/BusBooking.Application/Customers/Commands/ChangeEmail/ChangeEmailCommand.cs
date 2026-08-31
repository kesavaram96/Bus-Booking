using MediatR;

namespace BusBooking.Application.Customers.Commands.ChangeEmail;

public sealed record ChangeEmailCommand(Guid UserId, string Email) : IRequest;
