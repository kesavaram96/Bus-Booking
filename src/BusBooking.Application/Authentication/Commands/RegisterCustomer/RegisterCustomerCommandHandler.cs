using BusBooking.Application.Authentication.Common;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Authentication.Commands.RegisterCustomer;

public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RegisterCustomerCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResult> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var createResult = await _identityService.CreateCustomerAsync(
            request.FullName, request.Email, request.PhoneNumber, request.Password, cancellationToken);

        if (!createResult.Succeeded)
        {
            var failures = createResult.Errors.Select(error => new ValidationFailure(nameof(request.Email), error));
            throw new ValidationException(failures);
        }

        var user = await _identityService.FindByIdAsync(createResult.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Newly created user could not be found.");

        return await AuthResultFactory.CreateAsync(user, _jwtTokenService, _refreshTokenService, cancellationToken);
    }
}
