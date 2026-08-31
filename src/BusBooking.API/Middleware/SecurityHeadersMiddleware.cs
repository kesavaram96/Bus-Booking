namespace BusBooking.API.Middleware;

/// <summary>
/// Headers appropriate for a pure JSON API with no server-rendered HTML of its own (Swagger UI
/// is dev-only and already gated behind IsDevelopment() in Program.cs). Deliberately no
/// Content-Security-Policy here: CSP's value is almost entirely about controlling what an HTML
/// document is allowed to load/execute, which matters for the React SPA that will consume this
/// API — not for the API itself, which never renders markup for a browser to interpret.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
