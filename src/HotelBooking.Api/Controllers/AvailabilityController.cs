using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/hotels/{hotelId:int}/availability")]
public sealed class AvailabilityController : ControllerBase
{
    private readonly BookingDbContext _db;

    public AvailabilityController(BookingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Finds rooms available for the entire inclusive date range [from..to] for the given guest count.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AvailabilityResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResultDto>> Get(
        [FromRoute] int hotelId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? guests,
        CancellationToken ct)
    {
        // -------- Validation --------
        var errors = new Dictionary<string, string[]>();

        if (from is null) errors["from"] = new[] { "Query parameter 'from' is required (yyyy-MM-dd)." };
        if (to is null) errors["to"] = new[] { "Query parameter 'to' is required (yyyy-MM-dd)." };
        if (guests is null) errors["guests"] = new[] { "Query parameter 'guests' is required." };
        else if (guests <= 0) errors["guests"] = new[] { "'guests' must be at least 1." };

        if (from is not null && to is not null && to.Value < from.Value)
            errors["to"] = new[] { "'to' must be on or after 'from'." };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (from is not null && from.Value < today)
            errors["from"] = new[] { "The 'from' date must be today or a future date." };

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(errors)
            {
                Title = "Invalid query parameters",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problem);
        }

        var fromDate = from!.Value;
        var toDate = to!.Value;
        var guestCount = guests!.Value;

        // -------- Existence check --------
        var hotelExists = await _db.Hotels
            .AsNoTracking()
            .AnyAsync(h => h.Id == hotelId, ct);

        if (!hotelExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Hotel not found",
                Detail = $"No hotel exists with id '{hotelId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // -------- Availability query --------
        // A room is unavailable if it has ANY booking that overlaps:
        // existing.StartDate <= requested.To AND existing.EndDate >= requested.From
        var availableRooms = await _db.Rooms
            .AsNoTracking()
            .Where(r => r.HotelId == hotelId)
            .Where(r => r.Capacity >= guestCount)
            .Where(r => !_db.Bookings.Any(b =>
                b.RoomId == r.Id &&
                b.StartDate <= toDate &&
                b.EndDate >= fromDate))
            .OrderBy(r => r.Capacity)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new RoomAvailabilityDto(
                r.Id,
                r.RoomNumber,
                r.RoomType,
                r.Capacity))
            .ToListAsync(ct);

        var message = availableRooms.Count == 0
            ? "No rooms available for the given dates and guest count."
            : $"Found {availableRooms.Count} available room(s).";

        return Ok(new AvailabilityResultDto(
            HotelId: hotelId,
            From: fromDate.ToString("yyyy-MM-dd"),
            To: toDate.ToString("yyyy-MM-dd"),
            Guests: guestCount,
            AvailableRooms: availableRooms,
            Message: message));
    }
}
