using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Customers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Customers.Queries.GetCustomerProfile;

public sealed class GetCustomerProfileQueryHandler : IRequestHandler<GetCustomerProfileQuery, CustomerProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetCustomerProfileQueryHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<CustomerProfileDto> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // No Customer row yet is normal — registration doesn't create one (NIC/DateOfBirth
        // aren't collected then); it's created lazily on first profile update.
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.UserId, cancellationToken);

        return new CustomerProfileDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber ?? string.Empty,
            customer?.NIC,
            customer?.DateOfBirth,
            user.CreatedAt);
    }
}
