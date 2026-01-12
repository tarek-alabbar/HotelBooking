using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace HotelBooking.Api.Tests.Helpers;

/// <summary>
/// Provides convenience extension methods for HttpClient and HttpResponseMessage
/// to simplify common test operations such as GET, POST, and response validation.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a GET request to the specified URL, asserts a 200 OK response,
    /// and deserializes the JSON response body to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected response body type.</typeparam>
    /// <param name="client">The HttpClient used to send the request.</param>
    /// <param name="url">The relative URL to request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized response body.</returns>
    public static async Task<T> GetAndReadAsync<T>(this HttpClient client, string url, CancellationToken ct = default)
    {
        var response = await client.GetAsync(url, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        body.Should().NotBeNull();

        return body!;
    }

    /// <summary>
    /// Sends a POST request with no request body to the specified URL.
    /// Useful for admin endpoints such as reset and seed.
    /// </summary>
    /// <param name="client">The HttpClient used to send the request.</param>
    /// <param name="url">The relative URL to post to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    public static async Task<HttpResponseMessage> PostEmptyAsync(this HttpClient client, string url, CancellationToken ct = default)
        => await client.PostAsync(url, content: null, ct);

    /// <summary>
    /// Reads the status code and full response body as a string.
    /// Useful for debugging failed requests in assertions.
    /// </summary>
    /// <param name="response">The HTTP response to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the response status code and body text.</returns>
    public static async Task<(HttpStatusCode Status, string Content)> ReadStatusAndBodyAsync(this HttpResponseMessage response, CancellationToken ct = default)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, content);
    }
}