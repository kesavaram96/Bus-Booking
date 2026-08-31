namespace BusBooking.Application.Customers.DTOs;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
