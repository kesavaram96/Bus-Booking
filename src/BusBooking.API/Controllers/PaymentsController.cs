using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Payments.Commands.ConfirmPayment;
using BusBooking.Application.Payments.Commands.CreatePayment;
using BusBooking.Application.Payments.DTOs;
using BusBooking.Application.Payments.Queries.GetPaymentById;
using BusBooking.Application.Payments.Queries.GetPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Public, like Booking's own Create: a guest checkout never authenticates, and a Booking's
    /// Guid isn't sensitive on its own — creating or confirming a payment against it can't leak
    /// passenger data or move money anywhere unexpected (Cash is a manual staff attestation,
    /// the mock gateway never touches a real account), so no additional ownership proof is
    /// required yet. A real electronic provider integrated later would carry its own
    /// session/webhook security, handled entirely inside that IPaymentGateway implementation.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<PaymentDto>.SuccessResponse(result, "Payment created."));
    }

    [HttpPost("{id:guid}/confirm")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConfirmPaymentCommand(id), cancellationToken);
        return Ok(ApiResponse<PaymentDto>.SuccessResponse(result, "Payment confirmed."));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<PaymentDto>>>> GetPayments(
        [FromQuery] GetPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<PaymentDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPaymentByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<PaymentDto>.SuccessResponse(result));
    }
}
