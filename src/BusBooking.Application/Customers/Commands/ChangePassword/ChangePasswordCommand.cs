using MediatR;

namespace BusBooking.Application.Customers.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;
