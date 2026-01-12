using FluentAssertions;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace HotelBooking.Api.Tests.Tests;

/// <summary>
/// Integration tests for the hotel search endpoints.
/// </summary>
public sealed class HotelsTests : ApiTestBase
{
    /// <summary>
    /// Creates a new test class instance using the shared API factory.
    /// </summary>
    /// <param name="factory">The API test host factory.</param>
    public HotelsTests(ApiFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that searching by name returns at least one hotel and includes the expected seeded hotel.
    /// </summary>
    [Fact]
    public async Task Search_ByName_Should_Return_Matching_Hotel()
    {
        await ResetAndSeedAsync();

        var result = await Client.GetFromJsonAsync<SearchResultDto<HotelSummaryDto>>(
            $"/api/hotels?name={Uri.EscapeDataString(PrimarySeedHotelSearchTerm)}");

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Any(h => h.Name.Contains(PrimarySeedHotelSearchTerm, StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that searching for a hotel name that does not exist returns an empty items list
    /// and includes a human-friendly message.
    /// </summary>
    [Fact]
    public async Task Search_NonExisting_Should_Return_Empty_Items_With_Message()
    {
        await ResetAndSeedAsync();

        var result = await Client.GetFromJsonAsync<SearchResultDto<HotelSummaryDto>>("/api/hotels?name=DoesNotExist");
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that omitting the required "name" query parameter results in 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Search_WithoutName_Should_Return_400()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/hotels");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}