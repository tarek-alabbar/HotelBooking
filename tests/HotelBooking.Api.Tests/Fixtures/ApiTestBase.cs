using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.Tests.Helpers;
using HotelBooking.Api.Tests.Contracts;

namespace HotelBooking.Api.Tests.Fixtures;

public abstract class ApiTestBase : IClassFixture<ApiFactory>
{
    protected readonly HttpClient Client;

    protected ApiTestBase(ApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    protected async Task ResetAndSeedAsync(CancellationToken ct = default)
    {
        var reset = await Client.PostEmptyAsync("/api/admin/reset", ct);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var seed = await Client.PostEmptyAsync("/api/admin/seed", ct);
        seed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    protected async Task<int> GetHotelIdByNameAsync(string name, CancellationToken ct = default)
    {
        var result = await Client.GetFromJsonAsync<SearchResultDto<HotelSummaryDto>>($"/api/hotels?name={Uri.EscapeDataString(name)}", ct);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty($"seeded data should include hotel name containing '{name}'");

        return result.Items[0].Id;
    }

}