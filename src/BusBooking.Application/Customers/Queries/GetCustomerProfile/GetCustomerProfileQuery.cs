using BusBooking.Application.Customers.DTOs;
using MediatR;

namespace BusBooking.Application.Customers.Queries.GetCustomerProfile;

public sealed record GetCustomerProfileQuery(Guid UserId) : IRequest<CustomerProfileDto>;
