using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace HotelBooking.Api.Tests.Helpers;

public static class HttpClientExtensions
{
    public static async Task<T> GetAndReadAsync<T>(this HttpClient client, string url, CancellationToken ct = default)
    {
        var response = await client.GetAsync(url, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        body.Should().NotBeNull();
        return body!;
    }

    public static async Task<HttpResponseMessage> PostEmptyAsync(this HttpClient client, string url, CancellationToken ct = default)
        => await client.PostAsync(url, content: null, ct);

    public static async Task<(HttpStatusCode Status, string Content)> ReadStatusAndBodyAsync(this HttpResponseMessage response, CancellationToken ct = default)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, content);
    }
}