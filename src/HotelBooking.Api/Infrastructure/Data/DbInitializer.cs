using HotelBooking.Api.Domain.Entities;
using HotelBooking.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Infrastructure.Data;

public static class DbInitializer
{
    public sealed record SeedResult(int Hotels, int Rooms, int Bookings);

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
        // If already seeded, be explicit: don't duplicate data.
        if (await db.Hotels.AnyAsync(ct))
            return new SeedResult(0, 0, 0);

        var now = DateTime.UtcNow;

        // Two hotels to make "search by name" meaningful
        var hotel1 = new Hotel("Contoso Hotel London");
        var hotel2 = new Hotel("Fabrikam Grand Manchester");

        db.Hotels.AddRange(hotel1, hotel2);
        await db.SaveChangesAsync(ct);

        // Each hotel: exactly 6 rooms
        var rooms = new List<Room>();
        rooms.AddRange(CreateSixRooms(hotel1.Id));
        rooms.AddRange(CreateSixRooms(hotel2.Id));

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync(ct);

        // Create a couple bookings to test conflicts/availability deterministically.
        var h1Rooms = rooms.Where(r => r.HotelId == hotel1.Id).OrderBy(r => r.RoomNumber).ToList();

        var bookings = new List<Booking>
    {
        new Booking(
            bookingReference: "BK-000001",
            hotelId: hotel1.Id,
            roomId: h1Rooms[0].Id,
            startDate: new DateOnly(2026, 1, 10),
            endDate: new DateOnly(2026, 1, 12),
            guestCount: 1,
            createdUtc: now),

        new Booking(
            bookingReference: "BK-000002",
            hotelId: hotel1.Id,
            roomId: h1Rooms[3].Id,
            startDate: new DateOnly(2026, 1, 15),
            endDate: new DateOnly(2026, 1, 18),
            guestCount: 2,
            createdUtc: now)
    };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(ct);

        var nights = new List<BookingNight>();

        foreach (var booking in bookings)
        {
            foreach (var night in EnumerateNightsInclusive(booking.StartDate, booking.EndDate))
            {
                nights.Add(new BookingNight(booking.Id, booking.RoomId, night));
            }
        }

        db.BookingNights.AddRange(nights);
        await db.SaveChangesAsync(ct);

        return new SeedResult(
            Hotels: 2,
            Rooms: rooms.Count,
            Bookings: bookings.Count);
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
