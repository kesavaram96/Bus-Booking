using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Constants;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using BusBooking.Infrastructure.Persistence.DbContext;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class BusesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BusesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsOperationsManager_ReturnsCreatedBus()
    {
        await AuthenticateAsAsync(Roles.OperationsManager);

        var response = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            description = "49-seater coach",
            busType = "Luxury"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        body!.Data!.Status.Should().Be(BusStatus.Active);
        body.Data.BusType.Should().Be(BusType.Luxury);
    }

    [Fact]
    public async Task Create_WithDuplicateRegistrationNumber_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var registrationNumber = $"NB-{Guid.NewGuid():N}"[..12];

        await _client.PostAsJsonAsync("/api/buses", new { registrationNumber, busType = "Normal" });
        var response = await _client.PostAsJsonAsync("/api/buses", new { registrationNumber, busType = "Normal" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AsBookingStaff_ReturnsForbidden()
    {
        await AuthenticateAsAsync(Roles.BookingStaff);

        var response = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = $"NB-{Guid.NewGuid():N}"[..12],
            busType = "Normal"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_AsBookingStaff_ReturnsBus()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var created = await CreateBusAsync();

        await AuthenticateAsAsync(Roles.BookingStaff);
        var response = await _client.GetAsync($"/api/buses/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        body!.Data!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        await AuthenticateAsAsync(Roles.BookingStaff);

        var response = await _client.GetAsync($"/api/buses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ChangesBusDetails()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var created = await CreateBusAsync(busType: "Normal");

        var response = await _client.PutAsJsonAsync($"/api/buses/{created.Id}", new
        {
            registrationNumber = created.RegistrationNumber,
            description = "Updated description",
            busType = "AC"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        body!.Data!.BusType.Should().Be(BusType.AC);
        body.Data.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Deactivate_ThenActivate_TogglesStatus()
    {
        await AuthenticateAsAsync(Roles.OperationsManager);
        var created = await CreateBusAsync();

        var deactivateResponse = await _client.PatchAsync($"/api/buses/{created.Id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDeactivate = await GetBusAsync(created.Id);
        afterDeactivate.Status.Should().Be(BusStatus.Inactive);

        var activateResponse = await _client.PatchAsync($"/api/buses/{created.Id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterActivate = await GetBusAsync(created.Id);
        afterActivate.Status.Should().Be(BusStatus.Active);
    }

    [Fact]
    public async Task AssignSeatLayout_WithExistingLayout_UpdatesBus()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var created = await CreateBusAsync();
        var seatLayoutId = await SeedSeatLayoutAsync("49-Seater Standard");

        var response = await _client.PatchAsJsonAsync(
            $"/api/buses/{created.Id}/seat-layout",
            new { seatLayoutId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        body!.Data!.SeatLayoutId.Should().Be(seatLayoutId);
        body.Data.SeatLayoutName.Should().Be("49-Seater Standard");
    }

    [Fact]
    public async Task AssignSeatLayout_WithUnknownLayout_ReturnsNotFound()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var created = await CreateBusAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/buses/{created.Id}/seat-layout",
            new { seatLayoutId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBuses_FiltersByBusType()
    {
        await AuthenticateAsAsync(Roles.Admin);
        await CreateBusAsync(busType: "Luxury");
        await CreateBusAsync(busType: "SemiLuxury");

        var response = await _client.GetAsync("/api/buses?busType=Luxury&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<BusDto>>>(TestJsonOptions.Default);
        body!.Data!.Items.Should().NotBeEmpty();
        body.Data.Items.Should().OnlyContain(b => b.BusType == BusType.Luxury);
    }

    private async Task<BusDto> CreateBusAsync(string? registrationNumber = null, string busType = "Normal")
    {
        var response = await _client.PostAsJsonAsync("/api/buses", new
        {
            registrationNumber = registrationNumber ?? $"NB-{Guid.NewGuid():N}"[..12],
            busType
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<BusDto> GetBusAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/buses/{id}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BusDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<Guid> SeedSeatLayoutAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var seatLayout = new SeatLayout(name, null, rows: 13, columns: 4);
        context.SeatLayouts.Add(seatLayout);
        await context.SaveChangesAsync();

        return seatLayout.Id;
    }

    private async Task AuthenticateAsAsync(string role)
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
