using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace BusBooking.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CreateUserResult> CreateCustomerAsync(
        string fullName,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new CreateUserResult(false, Guid.Empty, ["Email is already registered."]);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return new CreateUserResult(false, Guid.Empty, result.Errors.Select(e => e.Description).ToArray());
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);

        return new CreateUserResult(true, user.Id, []);
    }

    public async Task<AuthenticatedUserDto?> ValidateCredentialsAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken)
    {
        var user = usernameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(usernameOrEmail)
            : await _userManager.FindByNameAsync(usernameOrEmail);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            if (_userManager.SupportsUserLockout)
            {
                await _userManager.AccessFailedAsync(user);
            }

            return null;
        }

        if (_userManager.SupportsUserLockout)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return await MapToDtoAsync(user);
    }

    public async Task<AuthenticatedUserDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return await MapToDtoAsync(user);
    }

    public async Task<bool> UpdateFullNameAsync(Guid userId, string fullName, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.FullName = fullName;
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<IdentityOperationResult> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new IdentityOperationResult(false, ["User not found."]);
        }

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing is not null && existing.Id != userId)
        {
            return new IdentityOperationResult(false, ["Email is already registered."]);
        }

        var emailResult = await _userManager.SetEmailAsync(user, newEmail);
        if (!emailResult.Succeeded)
        {
            return new IdentityOperationResult(false, emailResult.Errors.Select(e => e.Description).ToArray());
        }

        // UserName has followed Email since registration; keep them in sync so login still works.
        var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
        if (!userNameResult.Succeeded)
        {
            return new IdentityOperationResult(false, userNameResult.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success;
    }

    public async Task<IdentityOperationResult> ChangePhoneNumberAsync(
        Guid userId,
        string newPhoneNumber,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new IdentityOperationResult(false, ["User not found."]);
        }

        var result = await _userManager.SetPhoneNumberAsync(user, newPhoneNumber);

        return result.Succeeded
            ? IdentityOperationResult.Success
            : new IdentityOperationResult(false, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IdentityOperationResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new IdentityOperationResult(false, ["User not found."]);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded
            ? IdentityOperationResult.Success
            : new IdentityOperationResult(false, result.Errors.Select(e => e.Description).ToArray());
    }

    private async Task<AuthenticatedUserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new AuthenticatedUserDto(
            user.Id, user.UserName!, user.Email!, user.FullName, user.PhoneNumber, user.CreatedAt, roles.ToList());
    }
}
