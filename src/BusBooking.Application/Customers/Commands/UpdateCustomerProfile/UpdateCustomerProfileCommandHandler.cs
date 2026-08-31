using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Customers.DTOs;
using BusBooking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Customers.Commands.UpdateCustomerProfile;

public sealed class UpdateCustomerProfileCommandHandler
    : IRequestHandler<UpdateCustomerProfileCommand, CustomerProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public UpdateCustomerProfileCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<CustomerProfileDto> Handle(UpdateCustomerProfileCommand request, CancellationToken cancellationToken)
    {
        var updated = await _identityService.UpdateFullNameAsync(request.UserId, request.FullName, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException("User", request.UserId);
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.UserId, cancellationToken);
        if (customer is null)
        {
            // First profile update after registration — the Customer row doesn't exist yet.
            customer = new Customer(request.UserId, request.NIC, request.DateOfBirth);
            _context.Customers.Add(customer);
        }
        else
        {
            customer.UpdateProfile(request.NIC, request.DateOfBirth);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var user = await _identityService.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new CustomerProfileDto(
            user.Id, user.FullName, user.Email, user.PhoneNumber ?? string.Empty, customer.NIC, customer.DateOfBirth, user.CreatedAt);
    }
}
