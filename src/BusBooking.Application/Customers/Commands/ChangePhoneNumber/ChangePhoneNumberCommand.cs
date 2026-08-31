using MediatR;

namespace BusBooking.Application.Customers.Commands.ChangePhoneNumber;

public sealed record ChangePhoneNumberCommand(Guid UserId, string PhoneNumber) : IRequest;
