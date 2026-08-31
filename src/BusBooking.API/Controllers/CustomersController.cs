using System.Security.Claims;
using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Customers.Commands.ChangeEmail;
using BusBooking.Application.Customers.Commands.ChangePassword;
using BusBooking.Application.Customers.Commands.ChangePhoneNumber;
using BusBooking.Application.Customers.Commands.UpdateCustomerProfile;
using BusBooking.Application.Customers.DTOs;
using BusBooking.Application.Customers.Queries.GetCustomerProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

/// <summary>
/// Self-service only: every action is scoped to the authenticated caller's own profile via
/// the "sub" claim, never a route/body-supplied id, which is what actually enforces
/// "customers can only access their own profile" — there is no way to address anyone else's.
/// </summary>
[ApiController]
[Route("api/customers/me")]
[Authorize(Policy = AuthorizationPolicies.RequireCustomer)]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileDto>>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCustomerProfileQuery(CurrentUserId), cancellationToken);
        return Ok(ApiResponse<CustomerProfileDto>.SuccessResponse(result));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<CustomerProfileDto>>> UpdateProfile(
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerProfileCommand(CurrentUserId, request.FullName, request.NIC, request.DateOfBirth);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<CustomerProfileDto>.SuccessResponse(result, "Profile updated."));
    }

    [HttpPut("phone-number")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePhoneNumber(
        ChangePhoneNumberRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangePhoneNumberCommand(CurrentUserId, request.PhoneNumber), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Phone number updated."));
    }

    [HttpPut("email")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeEmail(
        ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeEmailCommand(CurrentUserId, request.Email), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Email updated."));
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangePasswordCommand(CurrentUserId, request.CurrentPassword, request.NewPassword), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password changed."));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
