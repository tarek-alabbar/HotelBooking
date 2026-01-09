using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Domain.Entities;
using HotelBooking.Api.Domain.Enums;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Application.Services;

public sealed class BookingService
{
    private readonly BookingDbContext _db;

    public BookingService(BookingDbContext db)
    {
        _db = db;
    }

    public sealed record BookingFailure(string Code, string Message);
    public sealed record BookingSuccess(BookingCreatedDto Result);

    public async Task<object> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct)
    {
        // ---- Validation ----
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.HotelId <= 0) return new BookingFailure("validation", "HotelId must be a positive integer.");
        if (request.Guests <= 0) return new BookingFailure("validation", "Guests must be at least 1.");
        if (request.To < request.From) return new BookingFailure("validation", "'to' must be on or after 'from'.");
        if (request.From < today) return new BookingFailure("validation", "'from' must be today or in the future.");

        var hotel = await _db.Hotels.AsNoTracking().FirstOrDefaultAsync(h => h.Id == request.HotelId, ct);
        if (hotel is null) return new BookingFailure("not_found", $"No hotel exists with id '{request.HotelId}'.");

        // Candidate rooms: best-fit first (smallest capacity that satisfies guests)
        var candidateRoomsQuery = _db.Rooms
            .AsNoTracking()
            .Where(r => r.HotelId == request.HotelId)
            .Where(r => r.Capacity >= request.Guests);

        if (request.RoomType is not null)
            candidateRoomsQuery = candidateRoomsQuery.Where(r => r.RoomType == request.RoomType.Value);

        var candidates = await candidateRoomsQuery
            .OrderBy(r => r.Capacity)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new { r.Id, r.RoomNumber, r.RoomType, r.Capacity })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return new BookingFailure("no_rooms", "No rooms match the guest count (and optional room type).");

        // We try rooms in order. If one conflicts (unique constraint on nights), try the next.
        foreach (var room in candidates)
        {
            // Each attempt is atomic.
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var bookingRef = GenerateBookingReference();

                var booking = new Booking(
                    bookingReference: bookingRef,
                    hotelId: request.HotelId,
                    roomId: room.Id,
                    startDate: request.From,
                    endDate: request.To,
                    guestCount: request.Guests,
                    createdUtc: DateTime.UtcNow);

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync(ct); // booking.Id now populated

                // Insert one row per night (inclusive)
                foreach (var night in EnumerateNightsInclusive(request.From, request.To))
                {
                    _db.BookingNights.Add(new BookingNight(booking.Id, room.Id, night));
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new BookingSuccess(new BookingCreatedDto(
                    BookingReference: booking.BookingReference,
                    HotelId: request.HotelId,
                    RoomId: room.Id,
                    RoomNumber: room.RoomNumber,
                    RoomType: room.RoomType,
                    Capacity: room.Capacity,
                    From: request.From.ToString("yyyy-MM-dd"),
                    To: request.To.ToString("yyyy-MM-dd"),
                    Guests: request.Guests
                ));
            }
            catch (DbUpdateException)
            {
                // Most likely: unique constraint violation on (RoomId, NightDate) OR booking reference collision.
                // Rollback and try next room.
                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();
            }
        }

        return new BookingFailure("conflict", "No rooms are available for the full requested date range.");
    }

    private static IEnumerable<DateOnly> EnumerateNightsInclusive(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    private static string GenerateBookingReference()
    {
        // Simple, readable, collision-resistant + unique index is final guard.
        // Example: BK-3F2A9C10D4E1
        return "BK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }
}
