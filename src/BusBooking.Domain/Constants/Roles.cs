namespace BusBooking.Domain.Constants;

public static class Roles
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Admin = nameof(Admin);
    public const string OperationsManager = nameof(OperationsManager);
    public const string BookingStaff = nameof(BookingStaff);
    public const string Driver = nameof(Driver);
    public const string Conductor = nameof(Conductor);
    public const string Customer = nameof(Customer);

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin, Admin, OperationsManager, BookingStaff, Driver, Conductor, Customer
    ];
}
