using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Domain.Constants;
using BusBooking.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace BusBooking.IntegrationTests.Controllers;

public class RoutesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoutesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_AsAdmin_ReturnsCreatedRoute()
    {
        await AuthenticateAsAsync(Roles.Admin);

        var response = await _client.PostAsJsonAsync("/api/routes", new
        {
            name = "Colombo - Jaffna",
            from = "Colombo",
            to = "Jaffna"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default);
        body!.Data!.IsActive.Should().BeFalse("a new route with no stops starts as a draft");
        body.Data.Stops.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithSameFromAndTo_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);

        var response = await _client.PostAsJsonAsync("/api/routes", new
        {
            name = "Invalid",
            from = "Colombo",
            to = "Colombo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_AsBookingStaff_ReturnsForbidden()
    {
        await AuthenticateAsAsync(Roles.BookingStaff);

        var response = await _client.PostAsJsonAsync("/api/routes", new { name = "X", from = "A", to = "B" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddStop_AutoAssignsSequentialOrder()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();

        var first = await AddStopAsync(route.Id, "Colombo");
        var second = await AddStopAsync(route.Id, "Kadawatha");

        first.StopOrder.Should().Be(1);
        second.StopOrder.Should().Be(2);
    }

    [Fact]
    public async Task AddStop_WithDuplicateName_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        await AddStopAsync(route.Id, "Colombo");

        var response = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new
        {
            stopName = "colombo",
            allowPickup = true,
            allowDropOff = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddStop_WithDepartureBeforeArrival_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();

        var response = await _client.PostAsJsonAsync($"/api/routes/{route.Id}/stops", new
        {
            stopName = "Kadawatha",
            expectedArrivalTime = "10:00:00",
            expectedDepartureTime = "09:00:00",
            allowPickup = true,
            allowDropOff = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Activate_WithFewerThanTwoStops_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        await AddStopAsync(route.Id, "Colombo");

        var response = await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Activate_WithTwoOrMoreStops_Succeeds()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        await AddStopAsync(route.Id, "Colombo");
        await AddStopAsync(route.Id, "Jaffna");

        var response = await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveStop_OnActiveRouteBelowTwoStops_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        var stop1 = await AddStopAsync(route.Id, "Colombo");
        await AddStopAsync(route.Id, "Jaffna");
        await _client.PatchAsync($"/api/routes/{route.Id}/activate", null);

        var response = await _client.DeleteAsync($"/api/routes/{route.Id}/stops/{stop1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveStop_OnInactiveRoute_AllowsGoingBelowTwoStops()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        var stop1 = await AddStopAsync(route.Id, "Colombo");
        await AddStopAsync(route.Id, "Jaffna");

        var response = await _client.DeleteAsync($"/api/routes/{route.Id}/stops/{stop1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReorderStops_SwappingFirstTwo_PersistsNewOrder()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        var colombo = await AddStopAsync(route.Id, "Colombo");
        var kadawatha = await AddStopAsync(route.Id, "Kadawatha");
        var kurunegala = await AddStopAsync(route.Id, "Kurunegala");

        var response = await _client.PutAsJsonAsync($"/api/routes/{route.Id}/stops/reorder", new
        {
            orderedStopIds = new[] { kadawatha.Id, colombo.Id, kurunegala.Id }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default);
        body!.Data!.Stops.Select(s => s.StopName).Should().ContainInOrder("Kadawatha", "Colombo", "Kurunegala");
        body.Data.Stops.Select(s => s.StopOrder).Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public async Task ReorderStops_WithIncompleteList_ReturnsBadRequest()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        var colombo = await AddStopAsync(route.Id, "Colombo");
        await AddStopAsync(route.Id, "Kadawatha");

        var response = await _client.PutAsJsonAsync($"/api/routes/{route.Id}/stops/reorder", new
        {
            orderedStopIds = new[] { colombo.Id }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStop_ChangesDetails()
    {
        await AuthenticateAsAsync(Roles.Admin);
        var route = await CreateRouteAsync();
        var stop = await AddStopAsync(route.Id, "Colombo");

        var response = await _client.PutAsJsonAsync($"/api/routes/{route.Id}/stops/{stop.Id}", new
        {
            stopName = "Colombo Fort",
            allowPickup = true,
            allowDropOff = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default);
        body!.Data!.StopName.Should().Be("Colombo Fort");
        body.Data.AllowDropOff.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveRoutes_OnlyReturnsActiveOnes()
    {
        await AuthenticateAsAsync(Roles.Admin);

        var activeRoute = await CreateRouteAsync();
        await AddStopAsync(activeRoute.Id, "Colombo");
        await AddStopAsync(activeRoute.Id, "Jaffna");
        await _client.PatchAsync($"/api/routes/{activeRoute.Id}/activate", null);

        var inactiveRoute = await CreateRouteAsync();

        var response = await _client.GetAsync("/api/routes/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RouteSummaryDto>>>(TestJsonOptions.Default);
        body!.Data!.Should().Contain(r => r.Id == activeRoute.Id);
        body.Data.Should().NotContain(r => r.Id == inactiveRoute.Id);
    }

    private async Task<RouteDto> CreateRouteAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync("/api/routes", new
        {
            name = $"Route-{suffix}",
            from = $"From-{suffix}",
            to = $"To-{suffix}"
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RouteDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task<RouteStopDto> AddStopAsync(Guid routeId, string stopName)
    {
        var response = await _client.PostAsJsonAsync($"/api/routes/{routeId}/stops", new
        {
            stopName,
            allowPickup = true,
            allowDropOff = true
        });

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RouteStopDto>>(TestJsonOptions.Default);
        return body!.Data!;
    }

    private async Task AuthenticateAsAsync(string role)
    {
        var accessToken = await _factory.CreateBusinessUserAndGetAccessTokenAsync(role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
