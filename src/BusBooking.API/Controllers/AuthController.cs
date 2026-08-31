using System.Security.Claims;
using BusBooking.Application.Authentication.Commands.Login;
using BusBooking.Application.Authentication.Commands.Logout;
using BusBooking.Application.Authentication.Commands.RefreshAccessToken;
using BusBooking.Application.Authentication.Commands.RegisterCustomer;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Authentication.Queries.GetCurrentUser;
using BusBooking.Application.Common.Models;
using BusBooking.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Register(
        RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Registration successful."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingServiceExtensions.LoginPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Login successful."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResult>>> RefreshToken(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<AuthResult>.SuccessResponse(result, "Token refreshed."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Logged out."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return Ok(ApiResponse<UserDto>.SuccessResponse(result));
    }
}
