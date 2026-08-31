using BusBooking.Application.Authentication.DTOs;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.RegisterCustomer;

public sealed record RegisterCustomerCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password) : IRequest<AuthResult>;
