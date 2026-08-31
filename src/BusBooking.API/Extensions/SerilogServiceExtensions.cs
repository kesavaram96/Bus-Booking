using Serilog;

namespace BusBooking.API.Extensions;

public static class SerilogServiceExtensions
{
    public static void UseSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "BusBooking.API");
        });
    }
}
