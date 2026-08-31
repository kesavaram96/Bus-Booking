namespace BusBooking.Application.Authentication.DTOs;

public sealed record UserDto(Guid Id, string UserName, string Email, string FullName, IReadOnlyList<string> Roles);
