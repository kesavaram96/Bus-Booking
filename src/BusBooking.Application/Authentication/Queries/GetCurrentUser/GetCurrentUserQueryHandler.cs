using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;

namespace BusBooking.Application.Authentication.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        return new UserDto(user.Id, user.UserName, user.Email, user.FullName, user.Roles);
    }
}
