using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Notifications;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Payments.Commands.ConfirmPayment;

/// <summary>
/// The one place a settlement attempt actually happens. Idempotent by design (the doc's explicit
/// requirement): a payment already Paid is returned as-is rather than re-charged, so a retried
/// client call or a redelivered gateway webhook can never double-charge, double-confirm the
/// booking, or double-generate tickets.
/// </summary>
public sealed class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, PaymentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IPaymentGateway> _paymentGateways;
    private readonly ITicketGenerationService _ticketGenerationService;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;

    public ConfirmPaymentCommandHandler(
        IApplicationDbContext context,
        IEnumerable<IPaymentGateway> paymentGateways,
        ITicketGenerationService ticketGenerationService,
        IIdentityService identityService,
        INotificationService notificationService)
    {
        _context = context;
        _paymentGateways = paymentGateways;
        _ticketGenerationService = ticketGenerationService;
        _identityService = identityService;
        _notificationService = notificationService;
    }

    public async Task<PaymentDto> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Payment", request.Id);

        if (payment.Status == PaymentStatus.Paid)
        {
            return payment.ToDto();
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.Id), "This payment can no longer be confirmed.")]);
        }

        var gateway = _paymentGateways.First(g => g.Supports(payment.PaymentMethod));
        var result = await gateway.ChargeAsync(
            new PaymentGatewayRequest(payment.Id, payment.Amount, payment.Currency, payment.PaymentMethod),
            cancellationToken);

        if (!result.Succeeded)
        {
            payment.MarkFailed();
            await _context.SaveChangesAsync(cancellationToken);

            throw new ValidationException([new ValidationFailure(nameof(request.Id), result.FailureReason ?? "Payment failed.")]);
        }

        payment.MarkPaid(result.TransactionReference!, DateTime.UtcNow);

        var booking = await _context.Bookings
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.Id == payment.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking", payment.BookingId);

        booking.Confirm();

        // Doc's Phase 15 requirement: tickets (with their QR-bound TicketCode) are generated
        // the moment a booking is confirmed, in the same transaction as the Payment/Booking
        // status changes above.
        await _ticketGenerationService.GenerateForBookingAsync(booking.Id, cancellationToken);

        var recipient = await BookingNotificationRecipientResolver.ResolveAsync(_identityService, booking, cancellationToken);
        if (recipient is not null)
        {
            await _notificationService.NotifyAsync(
                new NotificationRequest(
                    recipient.Value.Recipient,
                    recipient.Value.Channel,
                    NotificationEventType.BookingConfirmed,
                    "Your booking is confirmed",
                    $"Booking {booking.BookingNumber} is confirmed."),
                cancellationToken);

            await _notificationService.NotifyAsync(
                new NotificationRequest(
                    recipient.Value.Recipient,
                    recipient.Value.Channel,
                    NotificationEventType.PaymentSuccessful,
                    "Payment received",
                    $"Payment of {payment.Amount} {payment.Currency} for booking {booking.BookingNumber} was successful."),
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }
}
