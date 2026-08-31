using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Bus : Common.BaseAuditableEntity
{
    public string RegistrationNumber { get; private set; } = default!;

    public string? Description { get; private set; }

    public BusType BusType { get; private set; }

    public BusStatus Status { get; private set; }

    public Guid? SeatLayoutId { get; private set; }

    public SeatLayout? SeatLayout { get; private set; }

    private Bus()
    {
    }

    public Bus(string registrationNumber, string? description, BusType busType)
    {
        SetRegistrationNumber(registrationNumber);
        Description = description;
        BusType = busType;
        Status = BusStatus.Active;
    }

    public void UpdateDetails(string registrationNumber, string? description, BusType busType)
    {
        SetRegistrationNumber(registrationNumber);
        Description = description;
        BusType = busType;
    }

    public void AssignSeatLayout(Guid seatLayoutId)
    {
        if (seatLayoutId == Guid.Empty)
        {
            throw new ArgumentException("Seat layout id is required.", nameof(seatLayoutId));
        }

        SeatLayoutId = seatLayoutId;
    }

    public void Activate() => Status = BusStatus.Active;

    public void Deactivate() => Status = BusStatus.Inactive;

    private void SetRegistrationNumber(string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
        {
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        }

        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
    }
}
