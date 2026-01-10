using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;

namespace HotelBooking.Api.Tests.Tests;

public sealed class AdminEndpointsTests : ApiTestBase
{
    public AdminEndpointsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Reset_Then_Seed_Should_Return_Ok()
    {
        var reset = await Client.PostAsync("/api/admin/reset", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var seed = await Client.PostAsync("/api/admin/seed", null);
        seed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_GetAllBookings_Should_Return_List()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/admin/bookings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<BookingListItemDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().BeGreaterThan(0);
    }
}