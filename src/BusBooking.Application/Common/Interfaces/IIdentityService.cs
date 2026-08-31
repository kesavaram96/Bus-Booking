using BusBooking.Application.Common.Models;

namespace BusBooking.Application.Common.Interfaces;

/// <summary>
/// Abstracts ASP.NET Core Identity away from the Application layer.
/// </summary>
public interface IIdentityService
{
    Task<CreateUserResult> CreateCustomerAsync(
        string fullName,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticatedUserDto?> ValidateCredentialsAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticatedUserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <returns>false if no user with that id exists.</returns>
    Task<bool> UpdateFullNameAsync(Guid userId, string fullName, CancellationToken cancellationToken);

    Task<IdentityOperationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken);

    Task<IdentityOperationResult> ChangePhoneNumberAsync(Guid userId, string newPhoneNumber, CancellationToken cancellationToken);

    Task<IdentityOperationResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);
}

public sealed record CreateUserResult(bool Succeeded, Guid UserId, IReadOnlyList<string> Errors);

public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static readonly IdentityOperationResult Success = new(true, []);
}
