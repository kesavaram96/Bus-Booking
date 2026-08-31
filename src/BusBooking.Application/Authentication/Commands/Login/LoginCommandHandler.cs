using BusBooking.Application.Authentication.Common;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Interfaces;
using MediatR;

namespace BusBooking.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.UsernameOrEmail, request.Password, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid username/email or password.");

        return await AuthResultFactory.CreateAsync(user, _jwtTokenService, _refreshTokenService, cancellationToken);
    }
}
