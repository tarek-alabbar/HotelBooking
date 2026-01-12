using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HotelBooking.Api.Tests.Contracts;
using HotelBooking.Api.Tests.Fixtures;

namespace HotelBooking.Api.Tests.Tests;

/// <summary>
/// Integration tests for booking creation and booking lookup endpoints.
/// </summary>
public sealed class BookingTests : ApiTestBase
{
    /// <summary>
    /// Creates a new test class instance using the shared API factory.
    /// </summary>
    /// <param name="factory">The API test host factory.</param>
    public BookingTests(ApiFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that creating a booking returns a booking reference, and that the booking can then be
    /// retrieved by reference with consistent details.
    /// </summary>
    [Fact]
    public async Task CreateBooking_Then_LookupByReference_Should_Return_Details()
    {
        await ResetAndSeedAsync();

        var hotelId = await GetPrimarySeededHotelIdAsync();

        var createBody = new
        {
            hotelId,
            from = "2026-01-20",
            to = "2026-01-22",
            guests = 2,
            roomType = 2 // Double
        };

        var createResponse = await Client.PostAsJsonAsync("/api/bookings", createBody);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201 but got {(int)createResponse.StatusCode}. Body: {body}");
        }

        var created = await createResponse.Content.ReadFromJsonAsync<BookingCreatedDto>();
        created.Should().NotBeNull();
        created!.BookingReference.Should().NotBeNullOrWhiteSpace();

        var lookupResponse = await Client.GetAsync($"/api/bookings/{created.BookingReference}");
        lookupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await lookupResponse.Content.ReadFromJsonAsync<BookingDetailsDto>();
        details.Should().NotBeNull();
        details!.BookingReference.Should().Be(created.BookingReference);
        details.HotelId.Should().Be(hotelId);
        details.Guests.Should().Be(2);
        details.From.Should().Be("2026-01-20");
        details.To.Should().Be("2026-01-22");
    }

    /// <summary>
    /// Verifies that booking creation fails with 400 Bad Request when the guest count exceeds any room capacity.
    /// </summary>
    [Fact]
    public async Task CreateBooking_TooManyGuests_Should_Return_400()
    {
        await ResetAndSeedAsync();

        var createBody = new
        {
            hotelId = 1,
            from = "2026-01-20",
            to = "2026-01-20",
            guests = 99
        };

        var response = await Client.PostAsJsonAsync("/api/bookings", createBody);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that once all Double rooms are booked for the same date range,
    /// a subsequent booking request for a Double room returns 409 Conflict.
    /// </summary>
    [Fact]
    public async Task CreateBooking_WhenAllDoubleRoomsBooked_Should_Return_409()
    {
        await ResetAndSeedAsync();

        var hotelId = await GetPrimarySeededHotelIdAsync();

        // There are 2 Double rooms in seed (room numbers 3 and 4)
        var body = new
        {
            hotelId,
            from = "2026-01-25",
            to = "2026-01-26",
            guests = 2,
            roomType = 2 // Double
        };

        var r1 = await Client.PostAsJsonAsync("/api/bookings", body);
        if (r1.StatusCode != HttpStatusCode.Created)
        {
            var b1 = await r1.Content.ReadAsStringAsync();
            throw new Exception($"First booking expected 201 but got {(int)r1.StatusCode}. Body: {b1}");
        }

        var r2 = await Client.PostAsJsonAsync("/api/bookings", body);
        if (r2.StatusCode != HttpStatusCode.Created)
        {
            var b2 = await r2.Content.ReadAsStringAsync();
            throw new Exception($"Second booking expected 201 but got {(int)r2.StatusCode}. Body: {b2}");
        }

        var r3 = await Client.PostAsJsonAsync("/api/bookings", body);
        r3.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}