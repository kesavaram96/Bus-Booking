namespace BusBooking.Application.Common.Models;

/// <summary>
/// Identity-layer view of a user, returned by <see cref="Interfaces.IIdentityService"/>.
/// </summary>
public sealed record AuthenticatedUserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    string? PhoneNumber,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);
