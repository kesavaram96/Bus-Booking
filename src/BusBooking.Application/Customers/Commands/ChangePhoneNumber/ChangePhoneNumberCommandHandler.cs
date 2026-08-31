using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Customers.Commands.ChangePhoneNumber;

public sealed class ChangePhoneNumberCommandHandler : IRequestHandler<ChangePhoneNumberCommand>
{
    private readonly IIdentityService _identityService;

    public ChangePhoneNumberCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ChangePhoneNumberCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ChangePhoneNumberAsync(request.UserId, request.PhoneNumber, cancellationToken);

        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(error => new ValidationFailure(nameof(request.PhoneNumber), error));
            throw new ValidationException(failures);
        }
    }
}
