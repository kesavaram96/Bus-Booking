namespace BusBooking.Domain.Entities;

/// <summary>
/// Extends a registered customer's Identity account (Infrastructure.Identity.ApplicationUser)
/// with the profile fields Identity doesn't have. Id is the same value as the ApplicationUser's
/// Id (shared primary key, 1:1) — Domain cannot reference ApplicationUser directly since that
/// type lives in Infrastructure, so the link is a plain Guid, not a navigation property.
/// FullName/Email/PhoneNumber remain on ApplicationUser as the single source of truth.
/// </summary>
public class Customer : Common.BaseAuditableEntity
{
    public string? NIC { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    private Customer()
    {
    }

    public Customer(Guid userId, string? nic, DateOnly? dateOfBirth)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        Id = userId;
        NIC = nic;
        DateOfBirth = dateOfBirth;
    }

    public void UpdateProfile(string? nic, DateOnly? dateOfBirth)
    {
        NIC = nic;
        DateOfBirth = dateOfBirth;
    }
}
