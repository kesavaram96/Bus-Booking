using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IApplicationDbContext _context;

    public CreatePaymentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking", request.BookingId);

        if (booking.Status != BookingStatus.Pending)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.BookingId), "This booking is not awaiting payment.")]);
        }

        // One booking, at most one payment in flight or settled at a time — a Failed/Cancelled
        // attempt frees the booking up for a fresh one, but a Pending or Paid payment blocks a
        // second, preventing both a duplicate charge and the double-Confirm race that would
        // follow from two payments both settling for the same booking.
        var hasActivePayment = await _context.Payments.AnyAsync(
            p => p.BookingId == request.BookingId && (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Paid),
            cancellationToken);

        if (hasActivePayment)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(request.BookingId), "This booking already has a payment pending or completed.")]);
        }

        var payment = new Payment(booking.Id, booking.TotalAmount, "LKR", request.PaymentMethod);

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }
}
