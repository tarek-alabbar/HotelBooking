using HotelBooking.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Api.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly BookingDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminController(BookingDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        EnsureDevOrTest();

        await DbInitializer.ResetAsync(_db, ct);
        return Ok(new { message = "Database reset complete." });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        EnsureDevOrTest();

        var result = await DbInitializer.SeedAsync(_db, ct);
        return Ok(new
        {
            message = result is { Hotels: 0, Rooms: 0, Bookings: 0 }
                ? "Database already seeded (no changes made)."
                : "Database seeded successfully.",
            result
        });
    }


    [HttpGet("bookings")]
    [ProducesResponseType(typeof(List<BookingListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingListItemDto>>> GetAllBookings(CancellationToken ct)
    {
        EnsureDevOrTest();

        // Query only raw fields that translate cleanly to SQL.
        var raw = await _db.Bookings
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedUtc)
            .Join(_db.Hotels.AsNoTracking(),
                b => b.HotelId,
                h => h.Id,
                (b, h) => new { b, h })
            .Join(_db.Rooms.AsNoTracking(),
                bh => bh.b.RoomId,
                r => r.Id,
                (bh, r) => new
                {
                    bh.b.BookingReference,
                    HotelId = bh.h.Id,
                    HotelName = bh.h.Name,
                    r.RoomNumber,
                    r.RoomType,
                    Guests = bh.b.GuestCount,
                    bh.b.StartDate,
                    bh.b.EndDate,
                    bh.b.CreatedUtc
                })
            .ToListAsync(ct);

        // Map + format in-memory (safe across providers).
        var results = raw
            .Select(x => new BookingListItemDto(
                x.BookingReference,
                x.HotelId,
                x.HotelName,
                x.RoomNumber,
                x.RoomType,
                x.Guests,
                x.StartDate.ToString("yyyy-MM-dd"),
                x.EndDate.ToString("yyyy-MM-dd"),
                x.CreatedUtc.ToString("O")
            ))
            .ToList();

        return Ok(results);
    }


    private void EnsureDevOrTest()
    {
        // "Test" is commonly used by WebApplicationFactory.
        if (_env.IsDevelopment() || string.Equals(_env.EnvironmentName, "Test", StringComparison.OrdinalIgnoreCase))
            return;

        // Hide the existence of admin endpoints in non-dev environments.
        throw new InvalidOperationException("Admin endpoints are disabled outside Development/Test.");
    }
}
