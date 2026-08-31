namespace BusBooking.Domain.Entities;

public class Driver : Common.BaseAuditableEntity
{
    public string FullName { get; private set; } = default!;

    public string PhoneNumber { get; private set; } = default!;

    public string LicenseNumber { get; private set; } = default!;

    public DateOnly LicenseExpiryDate { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Driver()
    {
    }

    public Driver(string fullName, string phoneNumber, string licenseNumber, DateOnly licenseExpiryDate)
    {
        SetFullName(fullName);
        SetPhoneNumber(phoneNumber);
        SetLicenseNumber(licenseNumber);
        LicenseExpiryDate = licenseExpiryDate;
        IsActive = true;
    }

    public void UpdateDetails(string fullName, string phoneNumber, string licenseNumber, DateOnly licenseExpiryDate)
    {
        SetFullName(fullName);
        SetPhoneNumber(phoneNumber);
        SetLicenseNumber(licenseNumber);
        LicenseExpiryDate = licenseExpiryDate;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
    }

    private void SetPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        PhoneNumber = phoneNumber.Trim();
    }

    private void SetLicenseNumber(string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new ArgumentException("License number is required.", nameof(licenseNumber));
        }

        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
    }
}
