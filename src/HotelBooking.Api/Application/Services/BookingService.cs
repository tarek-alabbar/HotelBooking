using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Domain.Entities;
using HotelBooking.Api.Domain.Enums;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Application.Services;

public sealed class BookingService
{
    private readonly BookingDbContext _db;

    /// <summary>
    /// Creates a new BookingService.
    /// </summary>
    /// <param name="db">Database context used for persistence and queries.</param>
    public BookingService(BookingDbContext db)
    {
        _db = db;
    }

    public sealed record BookingFailure(string Code, string Message);
    public sealed record BookingSuccess(BookingCreatedDto Result);

    /// <summary>
    /// Attempts to create a booking for a single room across an inclusive date range.
    /// Applies validation, capacity constraints, optional room type filtering, and conflict avoidance.
    /// </summary>
    /// <param name="request">Booking request payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A success result containing booking details if a room is allocated,
    /// otherwise a failure result describing why a booking could not be made.
    /// </returns>
    public async Task<object> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.HotelId <= 0) return new BookingFailure("validation", "HotelId must be a positive integer.");
        if (request.Guests <= 0) return new BookingFailure("validation", "Guests must be at least 1.");
        if (request.To < request.From) return new BookingFailure("validation", "'to' must be on or after 'from'.");
        if (request.From < today) return new BookingFailure("validation", "'from' must be today or in the future.");

        var hotel = await _db.Hotels.AsNoTracking().FirstOrDefaultAsync(h => h.Id == request.HotelId, ct);
        if (hotel is null) return new BookingFailure("not_found", $"No hotel exists with id '{request.HotelId}'.");

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

        foreach (var room in candidates)
        {
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
                await _db.SaveChangesAsync(ct);

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
                // Try the next room candidate (conflict/uniqueness constraints indicate room not available).
                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();
            }
        }

        return new BookingFailure("conflict", "No rooms are available for the full requested date range.");
    }

    /// <summary>
    /// Enumerates an inclusive range of nights between two dates.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <returns>Sequence of nights represented as DateOnly values.</returns>
    private static IEnumerable<DateOnly> EnumerateNightsInclusive(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    /// <summary>
    /// Generates a collision-resistant booking reference.
    /// Uniqueness is ultimately enforced by a database unique index.
    /// </summary>
    /// <returns>A booking reference string. Example: BK-3F2A9C10D4E1</returns>
    private static string GenerateBookingReference()
    {
        return "BK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }
}
