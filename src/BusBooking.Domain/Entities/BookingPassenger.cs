using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class BookingPassenger : Common.BaseEntity
{
    public Guid BookingId { get; private set; }

    public string FullName { get; private set; } = default!;

    public string PhoneNumber { get; private set; } = default!;

    public Gender Gender { get; private set; }

    public string? NIC { get; private set; }

    public string? Email { get; private set; }

    public Guid PickupStopId { get; private set; }

    public RouteStop PickupStop { get; private set; } = default!;

    public Guid DropOffStopId { get; private set; }

    public RouteStop DropOffStop { get; private set; } = default!;

    public Guid SeatId { get; private set; }

    public Seat Seat { get; private set; } = default!;

    /// <summary>Server-calculated at booking time from the trip's fare — never client-supplied.</summary>
    public decimal Fare { get; private set; }

    private BookingPassenger()
    {
    }

    public BookingPassenger(
        Guid bookingId,
        string fullName,
        string phoneNumber,
        Gender gender,
        string? nic,
        string? email,
        Guid pickupStopId,
        Guid dropOffStopId,
        Guid seatId,
        decimal fare)
    {
        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException("Booking id is required.", nameof(bookingId));
        }

        if (pickupStopId == Guid.Empty)
        {
            throw new ArgumentException("Pickup stop id is required.", nameof(pickupStopId));
        }

        if (dropOffStopId == Guid.Empty)
        {
            throw new ArgumentException("Drop-off stop id is required.", nameof(dropOffStopId));
        }

        if (seatId == Guid.Empty)
        {
            throw new ArgumentException("Seat id is required.", nameof(seatId));
        }

        if (fare <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fare), "Fare must be greater than zero.");
        }

        SetFullName(fullName);
        SetPhoneNumber(phoneNumber);

        BookingId = bookingId;
        Gender = gender;
        NIC = nic;
        Email = email;
        PickupStopId = pickupStopId;
        DropOffStopId = dropOffStopId;
        SeatId = seatId;
        Fare = fare;
    }

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
}
