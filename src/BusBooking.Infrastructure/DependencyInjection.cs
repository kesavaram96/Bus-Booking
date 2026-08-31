using BusBooking.Application.Bookings;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Infrastructure.HealthChecks;
using BusBooking.Infrastructure.Identity;
using BusBooking.Infrastructure.Notifications;
using BusBooking.Infrastructure.Payments;
using BusBooking.Infrastructure.Persistence.DbContext;
using BusBooking.Infrastructure.Redis;
using BusBooking.Infrastructure.Security;
using BusBooking.Infrastructure.Tickets;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BusBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        var redisConnectionString = configuration.GetSection(RedisSettings.SectionName)["ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("Redis:ConnectionString was not found.");
        }

        // ConnectionMultiplexer is designed to be a single long-lived instance shared across
        // the app (not created per-request) — safe for multiple API instances since exclusivity
        // comes from Redis's own atomicity, not from anything held in this process.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<ISeatLockService, RedisSeatLockService>();

        // Both registered as IPaymentGateway; callers pick the one whose Supports() matches
        // the payment's method. Swap MockPaymentGateway for a real Sri Lankan provider here
        // when one is integrated — Cash is unaffected, and nothing above the Infrastructure
        // layer needs to change either way.
        services.AddScoped<IPaymentGateway, CashPaymentGateway>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();

        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

        services.Configure<CancellationPolicySettings>(configuration.GetSection(CancellationPolicySettings.SectionName));

        // Hangfire.SqlServer for real (shares the app's own database — Hangfire manages its
        // own schema there independently of EF Core migrations); Hangfire.MemoryStorage only
        // when the test host explicitly opts in via config, the same "swap the backing store
        // for tests" idea EF Core's InMemory provider already applies to ApplicationDbContext.
        var useMemoryJobStorage = configuration.GetValue<bool>("Hangfire:UseMemoryStorage");
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (useMemoryJobStorage)
            {
                config.UseMemoryStorage();
            }
            else
            {
                config.UseSqlServerStorage(connectionString);
            }
        });
        services.AddHangfireServer();

        // GlobalJobFilters is process-wide static state, and AddInfrastructureServices can run
        // more than once per process (e.g. one WebApplicationFactory host per integration test
        // class) — guarded so repeated startups don't stack up duplicate retry filters.
        if (!GlobalJobFilters.Filters.Select(f => f.Instance).OfType<AutomaticRetryAttribute>().Any())
        {
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [5, 15, 30] });
        }

        services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        // One registered per channel; NotificationDispatchJob picks whichever's Supports()
        // matches. Email is real (see EmailChannelSender); Sms/WhatsApp are the doc's explicit
        // placeholders, awaiting a real Sri Lankan gateway integration.
        services.AddScoped<INotificationChannelSender, EmailChannelSender>();
        services.AddScoped<INotificationChannelSender, SmsChannelSender>();
        services.AddScoped<INotificationChannelSender, WhatsAppChannelSender>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Database connectivity via EF Core's own health check (works against InMemory in
        // tests too — it just reports Healthy), Redis via a two-line PING against the same
        // IConnectionMultiplexer already registered above for seat locking.
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database")
            .AddCheck<RedisHealthCheck>("redis");

        return services;
    }
}
