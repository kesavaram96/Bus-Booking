using Microsoft.AspNetCore.Identity;

namespace BusBooking.Infrastructure.Identity;

/// <summary>
/// The Identity/login account. Distinct from the Domain.Entities.Customer profile
/// (FullName/Phone/Email/NIC/DateOfBirth) added in Phase 08, which links back here by Id.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
