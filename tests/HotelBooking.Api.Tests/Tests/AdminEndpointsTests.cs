using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.Tests.Fixtures;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Helpers;

namespace HotelBooking.Api.Tests.Tests;

/// <summary>
/// Integration tests covering the admin endpoints (reset, seed, and admin booking list).
/// </summary>
public sealed class AdminEndpointsTests : ApiTestBase
{
    /// <summary>
    /// Creates a new test class instance using the shared API factory.
    /// </summary>
    /// <param name="factory">The API test host factory.</param>
    public AdminEndpointsTests(ApiFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that the admin reset endpoint clears the database and that the seed endpoint
    /// can repopulate it successfully.
    /// </summary>
    [Fact]
    public async Task Reset_Then_Seed_Should_Return_Ok()
    {
        var reset = await Client.PostEmptyAsync("/api/admin/reset");
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var seed = await Client.PostEmptyAsync("/api/admin/seed");
        seed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the admin bookings endpoint returns at least one booking after seeding.
    /// </summary>
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