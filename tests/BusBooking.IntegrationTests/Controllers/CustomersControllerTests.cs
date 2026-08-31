using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Authentication.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Customers.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class CustomersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ValidPassword = "P@ssw0rd123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfile_ForNewlyRegisteredCustomer_HasNoNicOrDateOfBirthYet()
    {
        var (_, _) = await RegisterCustomerAsync();

        var response = await _client.GetAsync("/api/customers/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerProfileDto>>(TestJsonOptions.Default);
        body!.Data!.NIC.Should().BeNull();
        body.Data.DateOfBirth.Should().BeNull();
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/customers/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AsBusinessStaff_ReturnsForbidden()
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(Roles.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/customers/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_PersistsNicAndDateOfBirth()
    {
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/profile", new
        {
            fullName = "Updated Name",
            nic = "200012345678",
            dateOfBirth = "2000-01-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerProfileDto>>(TestJsonOptions.Default);
        body!.Data!.FullName.Should().Be("Updated Name");
        body.Data.NIC.Should().Be("200012345678");
        body.Data.DateOfBirth.Should().Be(new DateOnly(2000, 1, 15));

        // Persisted, not just echoed back.
        var refetched = await _client.GetAsync("/api/customers/me/profile");
        var refetchedBody = await refetched.Content.ReadFromJsonAsync<ApiResponse<CustomerProfileDto>>(TestJsonOptions.Default);
        refetchedBody!.Data!.NIC.Should().Be("200012345678");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidNic_ReturnsBadRequest()
    {
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/profile", new
        {
            fullName = "Someone",
            nic = "not-a-nic",
            dateOfBirth = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithFutureDateOfBirth_ReturnsBadRequest()
    {
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/profile", new
        {
            fullName = "Someone",
            nic = (string?)null,
            dateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1).ToString("yyyy-MM-dd")
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePhoneNumber_UpdatesProfile()
    {
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/phone-number", new { phoneNumber = "+94779998888" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await GetProfileAsync();
        profile.PhoneNumber.Should().Be("+94779998888");
    }

    [Fact]
    public async Task ChangeEmail_UpdatesProfileAndAllowsLoginWithNewEmail()
    {
        var (_, oldEmail) = await RegisterCustomerAsync();
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        var response = await _client.PutAsJsonAsync("/api/customers/me/email", new { email = newEmail });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await GetProfileAsync();
        profile.Email.Should().Be(newEmail);

        // UserName follows Email, so login must work with the new email...
        var loginWithNew = await _client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = newEmail, password = ValidPassword });
        loginWithNew.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...and the old one must no longer work.
        var loginWithOld = await _client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = oldEmail, password = ValidPassword });
        loginWithOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeEmail_ToAlreadyRegisteredEmail_ReturnsBadRequest()
    {
        var (_, existingEmail) = await RegisterCustomerAsync();
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/email", new { email = existingEmail });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_AllowsLoginWithNewPassword()
    {
        var (_, email) = await RegisterCustomerAsync();
        const string newPassword = "N3wP@ssword!";

        var response = await _client.PutAsJsonAsync("/api/customers/me/password", new
        {
            currentPassword = ValidPassword,
            newPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = email, password = newPassword });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        await RegisterCustomerAsync();

        var response = await _client.PutAsJsonAsync("/api/customers/me/password", new
        {
            currentPassword = "WrongPassword1",
            newPassword = "N3wP@ssword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TwoCustomers_EachOnlySeeOwnProfile()
    {
        var (_, emailA) = await RegisterCustomerAsync();
        await _client.PutAsJsonAsync("/api/customers/me/profile", new { fullName = "Customer A", nic = (string?)null, dateOfBirth = (string?)null });

        var (_, emailB) = await RegisterCustomerAsync();
        await _client.PutAsJsonAsync("/api/customers/me/profile", new { fullName = "Customer B", nic = (string?)null, dateOfBirth = (string?)null });

        var profileB = await GetProfileAsync();
        profileB.Email.Should().Be(emailB);
        profileB.FullName.Should().Be("Customer B");
        profileB.Email.Should().NotBe(emailA);
    }

    private async Task<CustomerProfileDto> GetProfileAsync()
    {
        var response = await _client.GetAsync("/api/customers/me/profile");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerProfileDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<(Guid UserId, string Email)> RegisterCustomerAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Customer",
            email,
            phoneNumber = "+94770000000",
            password = ValidPassword
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(TestJsonOptions.Default);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);

        return (body.Data.User.Id, email);
    }
}
