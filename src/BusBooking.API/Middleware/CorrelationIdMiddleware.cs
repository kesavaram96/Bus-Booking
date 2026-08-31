using Serilog.Context;

namespace BusBooking.API.Middleware;

/// <summary>
/// Runs first in the pipeline (see Program.cs), before Serilog's own request logging and
/// GlobalExceptionHandling, so every log line for a request — not just its error response —
/// carries the same correlation id. Accepts an incoming X-Correlation-Id header (so a caller or
/// gateway can thread its own id through), generating one only when the caller didn't supply
/// one; always echoed back on the response either way.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemsKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(ItemsKey, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
