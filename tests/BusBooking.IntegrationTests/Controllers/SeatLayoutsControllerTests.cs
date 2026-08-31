using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Common.Models;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class SeatLayoutsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SeatLayoutsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreatedLayout()
    {
        await AuthenticateAsAsync(Roles.Admin);

        var response = await _client.PostAsJsonAsync("/api/seat-layouts", new
        {
            name = "49-Seater Standard",
            description = "Default layout",
            rows = 13,
            columns = 4
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default);
        body!.Data!.Rows.Should().Be(13);
        body.Data.Seats.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_AsBookingStaff_ReturnsForbidden()
    {
        await AuthenticateAsAsync(Roles.BookingStaff);

        var response = await _client.PostAsJsonAsync("/api/seat-layouts", new { name = "X", rows = 2, columns = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddSeat_WithValidPosition_ReturnsSeat()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();

        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new
        {
            seatNumber = "01",
            row = 0,
            column = 0,
            positionType = "Seat"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>(TestJsonOptions.Default);
        body!.Data!.SeatNumber.Should().Be("01");
        body.Data.PositionType.Should().Be(Domain.Enums.SeatPositionType.Seat);
    }

    [Fact]
    public async Task AddSeat_OutsideLayoutBounds_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync(rows: 2, columns: 2);

        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new
        {
            seatNumber = "01",
            row = 5,
            column = 0,
            positionType = "Seat"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSeat_WithDuplicateSeatNumber_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();

        await AddSeatAsync(layout.Id, "01", 0, 0);
        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new
        {
            seatNumber = "01",
            row = 0,
            column = 1,
            positionType = "Seat"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSeat_WithDuplicatePosition_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();

        await AddSeatAsync(layout.Id, "01", 0, 0);
        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layout.Id}/seats", new
        {
            seatNumber = "02",
            row = 0,
            column = 0,
            positionType = "Seat"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSeatPosition_MovesSeat()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();
        var seat = await AddSeatAsync(layout.Id, "01", 0, 0);

        var response = await _client.PutAsJsonAsync(
            $"/api/seat-layouts/{layout.Id}/seats/{seat.Id}/position",
            new { row = 1, column = 2, positionType = "Seat" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>(TestJsonOptions.Default);
        body!.Data!.Row.Should().Be(1);
        body.Data.Column.Should().Be(2);
    }

    [Fact]
    public async Task UpdateSeatNumber_ChangesSeatNumber()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();
        var seat = await AddSeatAsync(layout.Id, "01", 0, 0);

        var response = await _client.PutAsJsonAsync(
            $"/api/seat-layouts/{layout.Id}/seats/{seat.Id}/number",
            new { seatNumber = "1A" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>(TestJsonOptions.Default);
        body!.Data!.SeatNumber.Should().Be("1A");
    }

    [Fact]
    public async Task DeactivateSeat_ThenActivate_TogglesIsActive()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();
        var seat = await AddSeatAsync(layout.Id, "01", 0, 0);

        var deactivateResponse = await _client.PatchAsync(
            $"/api/seat-layouts/{layout.Id}/seats/{seat.Id}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDeactivate = await GetLayoutAsync(layout.Id);
        afterDeactivate.Seats.Single().IsActive.Should().BeFalse();

        var activateResponse = await _client.PatchAsync(
            $"/api/seat-layouts/{layout.Id}/seats/{seat.Id}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterActivate = await GetLayoutAsync(layout.Id);
        afterActivate.Seats.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveSeat_RemovesFromLayout()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();
        var seat = await AddSeatAsync(layout.Id, "01", 0, 0);

        var response = await _client.DeleteAsync($"/api/seat-layouts/{layout.Id}/seats/{seat.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRemoval = await GetLayoutAsync(layout.Id);
        afterRemoval.Seats.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsSeatsOrderedByRowThenColumn()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync(rows: 5, columns: 5);

        await AddSeatAsync(layout.Id, "B", 1, 0);
        await AddSeatAsync(layout.Id, "A2", 0, 1);
        await AddSeatAsync(layout.Id, "A1", 0, 0);

        var result = await GetLayoutAsync(layout.Id);

        result.Seats.Select(s => s.SeatNumber).Should().ContainInOrder("A1", "A2", "B");
    }

    [Fact]
    public async Task GetSeatLayouts_ReturnsCreatedLayoutWithSeatCount()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var layout = await CreateLayoutAsync();
        await AddSeatAsync(layout.Id, "01", 0, 0);
        await AddSeatAsync(layout.Id, "02", 0, 1);

        var response = await _client.GetAsync("/api/seat-layouts?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<SeatLayoutSummaryDto>>>(TestJsonOptions.Default);
        var summary = body!.Data!.Items.Single(l => l.Id == layout.Id);
        summary.SeatCount.Should().Be(2);
    }

    private async Task<SeatLayoutDto> CreateLayoutAsync(int rows = 13, int columns = 4)
    {
        var response = await _client.PostAsJsonAsync("/api/seat-layouts", new
        {
            name = $"Layout-{Guid.NewGuid():N}",
            rows,
            columns
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<SeatDto> AddSeatAsync(Guid layoutId, string seatNumber, int row, int column)
    {
        var response = await _client.PostAsJsonAsync($"/api/seat-layouts/{layoutId}/seats", new
        {
            seatNumber,
            row,
            column,
            positionType = "Seat"
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<SeatLayoutDto> GetLayoutAsync(Guid id)
    {
        var response = await _client.GetAsync($"/api/seat-layouts/{id}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SeatLayoutDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task AuthenticateAsAsync(string role)
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
