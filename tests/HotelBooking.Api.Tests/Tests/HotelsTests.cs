using FluentAssertions;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;
using System.Net.Http.Json;

namespace HotelBooking.Api.Tests.Tests;

public sealed class HotelsTests : ApiTestBase
{
    public HotelsTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Search_ByName_Should_Return_Matching_Hotel()
    {
        await ResetAndSeedAsync();

        var result = await Client.GetFromJsonAsync<SearchResultDto<HotelSummaryDto>>("/api/hotels?name=Contoso");
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Any(h => h.Name.Contains("Contoso", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task Search_NonExisting_Should_Return_Empty_Items_With_Message()
    {
        await ResetAndSeedAsync();

        var result = await Client.GetFromJsonAsync<SearchResultDto<HotelSummaryDto>>("/api/hotels?name=DoesNotExist");
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Search_WithoutName_Should_Return_400()
    {
        await ResetAndSeedAsync();

        var response = await Client.GetAsync("/api/hotels");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}