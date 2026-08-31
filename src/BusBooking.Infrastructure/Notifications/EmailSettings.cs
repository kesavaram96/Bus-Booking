namespace BusBooking.Infrastructure.Notifications;

/// <summary>Bound from the "Email" configuration section.</summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "noreply@busbooking.lk";

    public string FromName { get; set; } = "BusBooking";

    /// <summary>Real SMTP relay host — set this for actual network delivery.</summary>
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseSsl { get; set; } = true;

    /// <summary>When set, every email is written as a .eml file here instead of sent over the
    /// network — local dev/test delivery with no SMTP server required. Takes priority over
    /// Host if both happen to be set.</summary>
    public string? PickupDirectory { get; set; }
}
