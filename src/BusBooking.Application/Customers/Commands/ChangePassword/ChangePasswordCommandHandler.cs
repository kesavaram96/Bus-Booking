using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Customers.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePasswordAsync(
            request.UserId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(error => new ValidationFailure(nameof(request.NewPassword), error));
            throw new ValidationException(failures);
        }
    }
}
