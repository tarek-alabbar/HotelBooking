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
    private readonly IConfiguration _config;
    /// <summary>
    /// Creates a controller providing Development/Test-only endpoints for data management and inspection.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="env">Host environment used to restrict endpoints outside Development/Test.</param>
    /// <param name="config">IConfiguration dependency injection.</param>
    public AdminController(BookingDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    /// <summary>
    /// Resets the database by removing all data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK if reset completes successfully.</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        EnsureDevOrTest();

        await DbInitializer.ResetAsync(_db, ct);
        return Ok(new { message = "Database reset complete." });
    }

    /// <summary>
    /// Seeds the database with a minimal deterministic dataset for testing.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with counts of inserted records (or a message if already seeded).</returns>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        EnsureDevOrTest();

        try
        {
            var result = await DbInitializer.SeedAsync(_db, ct);
            return Ok(new
            {
                message = result.Hotels == 0 ? "Database already seeded." : "Database seeded successfully.",
                result
            });
        }
        catch (Exception ex)
        {
            return Problem(
                title: "Seeding failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves all bookings for inspection in Development/Test.
    /// Useful for manual verification and automated tests.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of bookings ordered by most recent first.</returns>
    [HttpGet("bookings")]
    [ProducesResponseType(typeof(List<BookingListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingListItemDto>>> GetAllBookings(CancellationToken ct)
    {

        EnsureDevOrTest();

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

    /// <summary>
    /// Ensures that admin-only endpoints (seed, reset, admin booking list)
    /// are only accessible in Development, Test, or when explicitly enabled via configuration.
    /// This prevents dangerous operations from being exposed in Production by default.
    /// </summary>
    private void EnsureDevOrTest()
    {
        var explicitlyEnabled = _config.GetValue<bool>("AdminEndpoints:Enabled");

        if (explicitlyEnabled)
            return;

        if (_env.IsDevelopment())
            return;

        if (string.Equals(_env.EnvironmentName, "Test", StringComparison.OrdinalIgnoreCase))
            return;

        throw new InvalidOperationException(
            "Admin endpoints are disabled. Enable them by setting AdminEndpoints:Enabled=true.");
    }
}
