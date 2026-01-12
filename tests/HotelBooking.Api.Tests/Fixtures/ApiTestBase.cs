using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Helpers;

namespace HotelBooking.Api.Tests.Fixtures;

/// <summary>
/// Base class for API integration tests.
/// Provides a shared HttpClient plus helpers for reset/seed and seeded hotel lookup.
/// </summary>
public abstract class ApiTestBase : IClassFixture<ApiFactory>
{
    /// <summary>
    /// Default search term that should match at least one seeded hotel.
    /// Update this if seeded hotel names change.
    /// </summary>
    protected const string PrimarySeedHotelSearchTerm = "The Savoy Hotel London";

    /// <summary>
    /// HttpClient targeting the in-memory API host created by ApiFactory.
    /// </summary>
    protected readonly HttpClient Client;

    /// <summary>
    /// Creates the test base using the shared ApiFactory.
    /// </summary>
    /// <param name="factory">Factory used to create the in-memory test host and HttpClient.</param>
    protected ApiTestBase(ApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Resets and seeds the database using Development/Test admin endpoints.
    /// Ensures each test runs against a deterministic dataset.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    protected async Task ResetAndSeedAsync(CancellationToken ct = default)
    {
        var reset = await Client.PostEmptyAsync("/api/admin/reset", ct);
        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync(ct));

        var seed = await Client.PostEmptyAsync("/api/admin/seed", ct);
        seed.StatusCode.Should().Be(HttpStatusCode.OK, await seed.Content.ReadAsStringAsync(ct));
    }

    /// <summary>
    /// Finds a hotel ID by querying the hotel search endpoint.
    /// This avoids hardcoding database IDs that may vary between runs.
    /// </summary>
    /// <param name="name">Search term expected to match at least one hotel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the first matching hotel.</returns>
    protected async Task<int> GetHotelIdByNameAsync(string name, CancellationToken ct = default)
    {
        var url = $"/api/hotels?name={Uri.EscapeDataString(name)}";
        var response = await Client.GetAsync(url, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Hotel search should succeed. URL: {url}");

        var result = await response.Content.ReadFromJsonAsync<SearchResultDto<HotelSummaryDto>>(cancellationToken: ct);
        result.Should().NotBeNull();

        if (result!.Items.Count == 0)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"No hotels returned for search term '{name}'. Response body: {body}");
        }

        return result.Items[0].Id;
    }

    /// <summary>
    /// Gets the ID of the primary seeded hotel (used by most tests).
    /// Keeps tests resilient if the underlying seeded hotel ID changes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the primary seeded hotel.</returns>
    protected Task<int> GetPrimarySeededHotelIdAsync(CancellationToken ct = default)
        => GetHotelIdByNameAsync(PrimarySeedHotelSearchTerm, ct);
}