using System.Text.Json.Serialization;
using BusBooking.API.Extensions;
using BusBooking.API.Middleware;
using BusBooking.Application;
using BusBooking.Application.Notifications.Jobs;
using BusBooking.Infrastructure;
using BusBooking.Infrastructure.Identity;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.UseSerilogLogging();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsConfiguration(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPoliciesConfiguration();
builder.Services.AddRateLimitingConfiguration(builder.Configuration);

// HTTPS-ready for a containerized deployment: the API itself listens on plain HTTP inside the
// container (the .NET 8+ base image default), with TLS actually terminated by a reverse proxy/
// load balancer in front of it — a standard container pattern. Trusting X-Forwarded-*headers
// from that proxy is what lets UseHttpsRedirection, request logging, and the rate limiter's
// client-IP partitioning all see the real original scheme/IP instead of the proxy's own.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // No KnownProxies/KnownNetworks configured: the proxy sits in the same Docker network as
    // this container, whose address isn't fixed in advance the way a static reverse-proxy IP
    // would be — restrict this at the network/ingress layer instead of here.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// First in the pipeline (after resolving the real client from any proxy): every log line and
// every response from here on — including error responses — carries the same correlation id.
app.UseCorrelationId();

app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors(CorsServiceExtensions.ReactAppPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();
app.MapHealthCheckEndpoint();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// Registered here (after the app is built), not inside AddInfrastructureServices: Hangfire's
// static RecurringJob helper needs JobStorage.Current fully initialized and reachable, which
// design-time hosts (e.g. `dotnet ef migrations add`) never actually run — the service-based
// IRecurringJobManager avoids that failure mode entirely, per Hangfire's own recommendation.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<UpcomingTripReminderJob>(
        "upcoming-trip-reminders",
        job => job.RunAsync(CancellationToken.None),
        Cron.Hourly);
}

try
{
    Log.Information("Starting BusBooking API");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "BusBooking API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed for WebApplicationFactory-based integration tests.
/// </summary>
public partial class Program
{
}
