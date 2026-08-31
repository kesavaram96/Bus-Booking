using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsTokensAndCustomerRole()
    {
        var response = await RegisterAsync("Nimal Perera");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();

        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Data.User.Roles.Should().ContainSingle().Which.Should().Be("Customer");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = NewEmail();

        await RegisterAsync("Kamal Silva", email);
        var response = await RegisterAsync("Kamal Silva", email);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var email = NewEmail();
        await RegisterAsync("Sunil Fernando", email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { usernameOrEmail = email, password = ValidPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync("Amara Jayasuriya", email);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { usernameOrEmail = email, password = "WrongPassword1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUser()
    {
        var email = NewEmail();
        var registerBody = await RegisterAndReadAsync("Chamari Perera", email);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerBody.Data!.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        body!.Data!.Email.Should().Be(email);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_IssuesNewPairAndInvalidatesOldOne()
    {
        var registerBody = await RegisterAndReadAsync("Ruwan Bandara");
        var originalRefreshToken = registerBody.Data!.RefreshToken;

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = originalRefreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        refreshBody!.Data!.RefreshToken.Should().NotBe(originalRefreshToken);

        // A refresh token is single-use: replaying the original after rotation must fail.
        var reuseResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = originalRefreshToken });
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var registerBody = await RegisterAndReadAsync("Dilani Wickrama");
        var refreshToken = registerBody.Data!.RefreshToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerBody.Data.AccessToken);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.DefaultRequestHeaders.Authorization = null;

        var refreshAfterLogout = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken });
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private const string ValidPassword = "P@ssw0rd123";

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> RegisterAsync(string fullName, string? email = null) =>
        _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName,
            email = email ?? NewEmail(),
            phoneNumber = "+94770000000",
            password = ValidPassword
        });

    private async Task<ApiResponse<AuthResult>> RegisterAndReadAsync(string fullName, string? email = null)
    {
        var response = await RegisterAsync(fullName, email);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>())!;
    }
}
