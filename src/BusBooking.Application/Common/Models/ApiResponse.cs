namespace BusBooking.Application.Common.Models;

/// <summary>
/// Consistent envelope returned by every BusBooking API endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public T? Data { get; init; }

    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailureResponse(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
