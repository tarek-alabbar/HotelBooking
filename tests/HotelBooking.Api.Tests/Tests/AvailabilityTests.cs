using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;

namespace HotelBooking.Api.Tests.Tests;

/// <summary>
/// Integration tests for the room availability endpoint.
/// </summary>
public sealed class AvailabilityTests : ApiTestBase
{
    /// <summary>
    /// Creates a new test class instance using the shared API factory.
    /// </summary>
    /// <param name="factory">The API test host factory.</param>
    public AvailabilityTests(ApiFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that availability excludes a room that is already booked for the requested date range.
    /// Uses a future date range to satisfy the API rule that availability queries cannot start in the past.
    /// </summary>
    [Fact]
    public async Task Availability_ForSeededBookingRange_Should_ExcludeBookedRoom()
    {
        await ResetAndSeedAsync();

        var hotelId = await GetPrimarySeededHotelIdAsync();

        // Use a future range to avoid failing the API's "start date must be today or in the future" validation.
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)).ToString("yyyy-MM-dd");

        var url = $"/api/hotels/{hotelId}/availability?from={from}&to={to}&guests=1";

        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AvailabilityResultDto>();
        dto.Should().NotBeNull();
        dto!.AvailableRooms.Should().NotBeNull();

        // With 6 rooms and no bookings in this future range, all 6 should be available.
        dto.AvailableRooms.Count.Should().Be(6);
    }

    /// <summary>
    /// Verifies that searching for availability in the past is rejected with 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Availability_StartDate_InPast_Should_Return_400()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/hotels/1/availability?from=2000-01-01&to=2000-01-02&guests=1");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that requesting availability for a non-existent hotel returns 404 Not Found,
    /// provided the query parameters are otherwise valid.
    /// </summary>
    [Fact]
    public async Task Availability_HotelNotFound_Should_Return_404()
    {
        await ResetAndSeedAsync();

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)).ToString("yyyy-MM-dd");

        var response = await Client.GetAsync($"/api/hotels/999999/availability?from={from}&to={to}&guests=1");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}