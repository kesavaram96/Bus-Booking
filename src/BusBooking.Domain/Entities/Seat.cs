using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Seat : Common.BaseEntity
{
    public Guid SeatLayoutId { get; private set; }

    public string SeatNumber { get; private set; } = default!;

    public int Row { get; private set; }

    public int Column { get; private set; }

    public SeatPositionType PositionType { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Seat()
    {
    }

    public Seat(Guid seatLayoutId, string seatNumber, int row, int column, SeatPositionType positionType)
    {
        if (seatLayoutId == Guid.Empty)
        {
            throw new ArgumentException("Seat layout id is required.", nameof(seatLayoutId));
        }

        if (row < 0 || column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column must not be negative.");
        }

        SeatLayoutId = seatLayoutId;
        SetSeatNumber(seatNumber);
        Row = row;
        Column = column;
        PositionType = positionType;
        IsActive = true;
    }

    public void UpdateSeatNumber(string seatNumber) => SetSeatNumber(seatNumber);

    public void UpdatePosition(int row, int column, SeatPositionType positionType)
    {
        if (row < 0 || column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row and column must not be negative.");
        }

        Row = row;
        Column = column;
        PositionType = positionType;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetSeatNumber(string seatNumber)
    {
        if (string.IsNullOrWhiteSpace(seatNumber))
        {
            throw new ArgumentException("Seat number is required.", nameof(seatNumber));
        }

        SeatNumber = seatNumber.Trim();
    }
}
