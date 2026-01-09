using HotelBooking.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

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

    private void EnsureDevOrTest()
    {
        // "Test" is commonly used by WebApplicationFactory.
        if (_env.IsDevelopment() || string.Equals(_env.EnvironmentName, "Test", StringComparison.OrdinalIgnoreCase))
            return;

        // Hide the existence of admin endpoints in non-dev environments.
        throw new InvalidOperationException("Admin endpoints are disabled outside Development/Test.");
    }
}
