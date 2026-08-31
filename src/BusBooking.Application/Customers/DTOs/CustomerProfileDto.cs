namespace BusBooking.Application.Customers.DTOs;

/// <summary>
/// Combines the Identity account (FullName/Email/PhoneNumber) with the Customer profile
/// extension (NIC/DateOfBirth). Deliberately excludes password hashes and any other
/// security-related field.
/// </summary>
public sealed record CustomerProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    string? NIC,
    DateOnly? DateOfBirth,
    DateTime CreatedAt);
