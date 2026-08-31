namespace BusBooking.Domain.Entities;

public class Route : Common.BaseAuditableEntity
{
    private readonly List<RouteStop> _stops = [];

    public string Name { get; private set; } = default!;

    public string From { get; private set; } = default!;

    public string To { get; private set; } = default!;

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<RouteStop> Stops => _stops.AsReadOnly();

    private Route()
    {
    }

    /// <summary>
    /// A new route starts inactive (draft): it has no stops yet, so it cannot be used for
    /// trips until stops are added and it is explicitly activated.
    /// </summary>
    public Route(string name, string from, string to)
    {
        SetName(name);
        SetFromTo(from, to);
        IsActive = false;
    }

    public void UpdateDetails(string name, string from, string to)
    {
        SetName(name);
        SetFromTo(from, to);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void AddStop(RouteStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        _stops.Add(stop);
    }

    public void RemoveStop(RouteStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        _stops.Remove(stop);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Route name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetFromTo(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from))
        {
            throw new ArgumentException("From is required.", nameof(from));
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("To is required.", nameof(to));
        }

        if (string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("From and To must be different.", nameof(to));
        }

        From = from.Trim();
        To = to.Trim();
    }
}
