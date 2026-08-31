namespace BusBooking.Application.Common.Interfaces;

/// <summary>Abstracts Hangfire away from the Application layer — the same reasoning as every
/// other Infrastructure-backed interface here (ISeatLockService, IPaymentGateway, ...).</summary>
public interface IBackgroundJobScheduler
{
    void EnqueueNotificationDispatch(Guid notificationLogId);
}
