using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;

namespace HotelBooking.Api.Tests.Tests;

public sealed class AvailabilityTests : ApiTestBase
{
    public AvailabilityTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Availability_ForSeededBookingRange_Should_ExcludeBookedRoom()
    {
        await ResetAndSeedAsync();

        var hotelId = await GetHotelIdByNameAsync("Contoso");

        // Seed has a booking in Contoso on RoomNumber 1 for 2026-01-10..2026-01-12
        var url = $"/api/hotels/{hotelId}/availability?from=2026-01-10&to=2026-01-12&guests=1";

        var response = await Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<AvailabilityResultDto>();
        dto.Should().NotBeNull();
        dto!.AvailableRooms.Should().NotBeNull();

        // Hotel has 6 rooms, one is booked for that range => expect 5 available
        dto.AvailableRooms.Count.Should().Be(5);
        dto.AvailableRooms.Any(r => r.RoomNumber == 1).Should().BeFalse();
    }


    [Fact]
    public async Task Availability_StartDate_InPast_Should_Return_400()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/hotels/1/availability?from=2000-01-01&to=2000-01-02&guests=1");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Availability_HotelNotFound_Should_Return_404()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/hotels/999/availability?from=2026-01-10&to=2026-01-10&guests=1");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}