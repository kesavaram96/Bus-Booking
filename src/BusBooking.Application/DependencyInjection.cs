using System.Reflection;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Notifications.Jobs;
using BusBooking.Application.Notifications.Services;
using BusBooking.Application.Tickets.Services;
using FluentValidation;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
            // Registered after ValidationBehavior so it wraps *inside* it — an invalid request
            // never reaches this and is never audited.
            config.AddOpenBehavior(typeof(Common.Behaviors.AuditLoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        var mapperConfig = new Mapster.TypeAdapterConfig();
        mapperConfig.Scan(applicationAssembly);
        services.AddSingleton(mapperConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        // Pure business logic (only needs IApplicationDbContext, no infrastructure dependency),
        // so it's implemented directly in Application rather than split across an interface
        // here and an implementation in Infrastructure.
        services.AddScoped<ITicketGenerationService, TicketGenerationService>();

        // Same reasoning: NotificationService only needs IApplicationDbContext and
        // IBackgroundJobScheduler, both already Application-level abstractions. The dispatch
        // job Hangfire actually invokes is registered too — Hangfire's job activator resolves
        // it from this same container regardless of which layer it's defined in.
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<NotificationDispatchJob>();
        services.AddScoped<UpcomingTripReminderJob>();

        return services;
    }
}
