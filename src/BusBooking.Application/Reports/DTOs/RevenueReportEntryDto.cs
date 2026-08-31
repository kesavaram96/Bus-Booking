namespace BusBooking.Application.Reports.DTOs;

/// <summary>One row per calendar day (grouped by Payment.PaidAt — when money actually came in,
/// not when the Payment record was created), counting only Paid payments.</summary>
public sealed record RevenueReportEntryDto(DateOnly Date, int PaymentCount, decimal TotalRevenue);
