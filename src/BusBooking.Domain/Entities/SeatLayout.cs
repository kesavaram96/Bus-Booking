namespace BusBooking.Domain.Entities;

public class SeatLayout : Common.BaseAuditableEntity
{
    private readonly List<Seat> _seats = [];

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public int Rows { get; private set; }

    public int Columns { get; private set; }

    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    private SeatLayout()
    {
    }

    public SeatLayout(string name, string? description, int rows, int columns)
    {
        SetName(name);
        Description = description;
        SetDimensions(rows, columns);
    }

    public void UpdateDetails(string name, string? description, int rows, int columns)
    {
        SetName(name);
        Description = description;
        SetDimensions(rows, columns);
    }

    public void AddSeat(Seat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        _seats.Add(seat);
    }

    public void RemoveSeat(Seat seat)
    {
        ArgumentNullException.ThrowIfNull(seat);
        _seats.Remove(seat);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Seat layout name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetDimensions(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be greater than zero.");
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");
        }

        Rows = rows;
        Columns = columns;
    }
}
