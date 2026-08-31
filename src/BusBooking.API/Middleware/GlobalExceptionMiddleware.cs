using System.Net;
using System.Text.Json;
using BusBooking.Application.Common.Exceptions;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.API.Middleware;

/// <summary>
/// Translates unhandled exceptions into a consistent JSON error response
/// so no controller needs its own try/catch for cross-cutting failures.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var value)
            ? value?.ToString()
            : context.TraceIdentifier;

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                (IDictionary<string, string[]>?)validationException.Errors),

            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                notFoundException.Message,
                null),

            ForbiddenAccessException forbiddenAccessException => (
                HttpStatusCode.Forbidden,
                forbiddenAccessException.Message,
                null),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "You are not authorized to perform this action.",
                null),

            // Domain entities use this for state-machine guards (e.g. Trip status transitions:
            // "only a scheduled trip can be marked as boarding") — an invalid-state error is a
            // client error (400), not a server fault.
            InvalidOperationException invalidOperationException => (
                HttpStatusCode.BadRequest,
                invalidOperationException.Message,
                null),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.",
                null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception. CorrelationId: {CorrelationId}", correlationId);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            success = false,
            message = title,
            errors,
            correlationId,
            detail = _environment.IsDevelopment() ? exception.ToString() : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        }));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
