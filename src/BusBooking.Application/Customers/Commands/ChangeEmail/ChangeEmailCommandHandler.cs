using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Customers.Commands.ChangeEmail;

public sealed class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand>
{
    private readonly IIdentityService _identityService;

    public ChangeEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangeEmailAsync(request.UserId, request.Email, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(error => new ValidationFailure(nameof(request.Email), error));
            throw new ValidationException(failures);
        }
    }
}
