using HotelBooking.Api.Domain.Entities;
using HotelBooking.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Infrastructure.Data;

public static class DbInitializer
{
    /// <summary>
    /// Result summary returned by the seed operation.
    /// </summary>
    public sealed record SeedResult(int Hotels, int Rooms, int Bookings, int BookingNights);

    /// <summary>
    /// Deletes all data from the database in a child-to-parent order to avoid FK issues.
    /// Intended for Development/Test only.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task ResetAsync(BookingDbContext db, CancellationToken ct = default)
    {
        // Delete children first to avoid FK issues.
        await db.BookingNights.ExecuteDeleteAsync(ct);
        await db.Bookings.ExecuteDeleteAsync(ct);
        await db.Rooms.ExecuteDeleteAsync(ct);
        await db.Hotels.ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Seeds the database with a minimal deterministic dataset for manual testing and automated tests.
    /// Seeding is idempotent: if hotels already exist, no changes are made.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Counts of created entities (0s if already seeded).</returns>
    public static async Task<SeedResult> SeedAsync(BookingDbContext db, CancellationToken ct = default)
    {
        // Idempotency: avoid duplicating data
        if (await db.Hotels.AnyAsync(ct))
            return new SeedResult(0, 0, 0, 0);

        var now = DateTime.UtcNow;

        // Hotels: keep names stable for tests and manual verification
        var hotels = new[]
        {
            new Hotel("The Savoy Hotel London"),
            new Hotel("Grand Central Hotel Glasgow"),
            new Hotel("The Balmoral Hotel Edinburgh")
        };

        db.Hotels.AddRange(hotels);
        await db.SaveChangesAsync(ct);

        // Rooms: exactly 6 per hotel
        var rooms = hotels
            .SelectMany(h => CreateSixRooms(h.Id))
            .ToList();

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync(ct);

        // Bookings: seed a few deterministic scenarios
        var contoso = hotels.Single(h => h.Name.StartsWith("Contoso", StringComparison.OrdinalIgnoreCase));
        var contosoRooms = rooms
            .Where(r => r.HotelId == contoso.Id)
            .OrderBy(r => r.RoomNumber)
            .ToList();

        var bookings = new List<Booking>
        {
            // Occupies RoomNumber 1 for 2026-01-10..2026-01-12
            new Booking(
                bookingReference: "BK-000001",
                hotelId: contoso.Id,
                roomId: contosoRooms[0].Id,
                startDate: new DateOnly(2026, 1, 10),
                endDate: new DateOnly(2026, 1, 12),
                guestCount: 1,
                createdUtc: now),

            // Occupies RoomNumber 4 for 2026-01-15..2026-01-18
            new Booking(
                bookingReference: "BK-000002",
                hotelId: contoso.Id,
                roomId: contosoRooms[3].Id,
                startDate: new DateOnly(2026, 1, 15),
                endDate: new DateOnly(2026, 1, 18),
                guestCount: 2,
                createdUtc: now),
        };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(ct);

        // BookingNights:
        // We treat [StartDate..EndDate] as an inclusive stay range in this solution,
        // so each date in that range becomes an occupied night for the room.
        var bookingNights = bookings
            .SelectMany(b => EnumerateNightsInclusive(b.StartDate, b.EndDate)
                .Select(night => new BookingNight(b.Id, b.RoomId, night)))
            .ToList();

        db.BookingNights.AddRange(bookingNights);
        await db.SaveChangesAsync(ct);

        return new SeedResult(
            Hotels: hotels.Length,
            Rooms: rooms.Count,
            Bookings: bookings.Count,
            BookingNights: bookingNights.Count);
    }

    /// <summary>
    /// Creates exactly six rooms for a hotel, distributed across room types with deterministic capacities.
    /// </summary>
    /// <param name="hotelId">The owning hotel identifier.</param>
    /// <returns>A fixed set of six rooms.</returns>
    private static IEnumerable<Room> CreateSixRooms(int hotelId)
    {
        return new[]
        {
            new Room(hotelId, roomNumber: 1, roomType: RoomType.Single, capacity: 1),
            new Room(hotelId, roomNumber: 2, roomType: RoomType.Single, capacity: 1),
            new Room(hotelId, roomNumber: 3, roomType: RoomType.Double, capacity: 2),
            new Room(hotelId, roomNumber: 4, roomType: RoomType.Double, capacity: 2),
            new Room(hotelId, roomNumber: 5, roomType: RoomType.Deluxe, capacity: 4),
            new Room(hotelId, roomNumber: 6, roomType: RoomType.Deluxe, capacity: 4),
        };
    }

    /// <summary>
    /// Enumerates an inclusive range of nights between two dates.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <returns>Sequence of dates representing occupied nights.</returns>
    private static IEnumerable<DateOnly> EnumerateNightsInclusive(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }
}
