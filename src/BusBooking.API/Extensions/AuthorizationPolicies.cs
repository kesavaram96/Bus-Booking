using BusBooking.Domain.Constants;

namespace BusBooking.API.Extensions;

public static class AuthorizationPolicies
{
    public const string RequireSuperAdmin = nameof(RequireSuperAdmin);
    public const string RequireAdminOrAbove = nameof(RequireAdminOrAbove);
    public const string RequireOperationsStaff = nameof(RequireOperationsStaff);
    public const string RequireBookingStaff = nameof(RequireBookingStaff);
    public const string RequireCustomer = nameof(RequireCustomer);
    public const string RequireBookingStaffOrCustomer = nameof(RequireBookingStaffOrCustomer);

    public static IServiceCollection AddAuthorizationPoliciesConfiguration(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(RequireSuperAdmin, policy => policy.RequireRole(Roles.SuperAdmin))
            .AddPolicy(RequireAdminOrAbove, policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin))
            .AddPolicy(RequireOperationsStaff, policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.OperationsManager))
            .AddPolicy(RequireBookingStaff, policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.OperationsManager, Roles.BookingStaff))
            .AddPolicy(RequireCustomer, policy => policy.RequireRole(Roles.Customer))
            // Booking cancellation: staff can cancel any booking, a Customer can cancel their
            // own (ownership checked in the handler, since that needs the loaded Booking).
            .AddPolicy(RequireBookingStaffOrCustomer, policy => policy.RequireRole(
                Roles.SuperAdmin, Roles.Admin, Roles.OperationsManager, Roles.BookingStaff, Roles.Customer));

        return services;
    }
}
