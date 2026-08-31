using BusBooking.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Infrastructure.Identity;

/// <summary>
/// Ensures the fixed role set exists, and optionally bootstraps a SuperAdmin account when
/// Seed:SuperAdminEmail/Seed:SuperAdminPassword are configured (via user-secrets or
/// environment variables — never hard-coded). Safe to run on every startup: both steps
/// check for existing data first.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        var superAdminEmail = configuration["Seed:SuperAdminEmail"];
        var superAdminPassword = configuration["Seed:SuperAdminPassword"];

        if (string.IsNullOrWhiteSpace(superAdminEmail) || string.IsNullOrWhiteSpace(superAdminPassword))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(superAdminEmail) is not null)
        {
            return;
        }

        var superAdmin = new ApplicationUser
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        }
    }
}
