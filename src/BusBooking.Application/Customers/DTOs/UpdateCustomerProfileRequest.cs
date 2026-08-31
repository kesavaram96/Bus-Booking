namespace BusBooking.Application.Customers.DTOs;

public sealed record UpdateCustomerProfileRequest(string FullName, string? NIC, DateOnly? DateOfBirth);
