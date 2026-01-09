using HotelBooking.Api.Domain.Entities;
using HotelBooking.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Infrastructure.Data;

public static class DbInitializer
{
    public sealed record SeedResult(int Hotels, int Rooms, int Bookings);

    public static async Task ResetAsync(BookingDbContext db, CancellationToken ct = default)
    {
        // Delete children first to avoid FK issues.
        await db.Bookings.ExecuteDeleteAsync(ct);
        await db.Rooms.ExecuteDeleteAsync(ct);
        await db.Hotels.ExecuteDeleteAsync(ct);
    }

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
        await db.SaveChangesAsync(ct); // get IDs for FK relationships

        // Each hotel: exactly 6 rooms
        // Distribution: 2 Single (1), 2 Double (2), 2 Deluxe (4)
        var rooms = new List<Room>();

        rooms.AddRange(CreateSixRooms(hotel1.Id));
        rooms.AddRange(CreateSixRooms(hotel2.Id));

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync(ct);

        // Create a couple bookings to test conflicts/availability deterministically.
        // Inclusive nights: [StartDate..EndDate]
        // Example: booking occupies nights 10,11,12 Jan.
        var h1Rooms = rooms.Where(r => r.HotelId == hotel1.Id).OrderBy(r => r.RoomNumber).ToList();

        var bookings = new List<Booking>
        {
            new Booking(
                bookingReference: "BK-000001",
                hotelId: hotel1.Id,
                roomId: h1Rooms[0].Id, // RoomNumber 1
                startDate: new DateOnly(2026, 1, 10),
                endDate: new DateOnly(2026, 1, 12),
                guestCount: 1,
                createdUtc: now),

            new Booking(
                bookingReference: "BK-000002",
                hotelId: hotel1.Id,
                roomId: h1Rooms[3].Id, // RoomNumber 4 (likely a Double/Deluxe depending on distribution)
                startDate: new DateOnly(2026, 1, 15),
                endDate: new DateOnly(2026, 1, 18),
                guestCount: 2,
                createdUtc: now)
        };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(ct);

        return new SeedResult(
            Hotels: 2,
            Rooms: rooms.Count,
            Bookings: bookings.Count);
    }

    private static IEnumerable<Room> CreateSixRooms(int hotelId)
    {
        // RoomNumbers 1..6
        // 1-2: Single (cap 1)
        // 3-4: Double (cap 2)
        // 5-6: Deluxe (cap 4)
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
}
