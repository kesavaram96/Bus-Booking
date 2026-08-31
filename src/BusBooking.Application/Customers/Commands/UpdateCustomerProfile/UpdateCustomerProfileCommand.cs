using BusBooking.Application.Customers.DTOs;
using MediatR;

namespace BusBooking.Application.Customers.Commands.UpdateCustomerProfile;

public sealed record UpdateCustomerProfileCommand(
    Guid UserId,
    string FullName,
    string? NIC,
    DateOnly? DateOfBirth) : IRequest<CustomerProfileDto>;
