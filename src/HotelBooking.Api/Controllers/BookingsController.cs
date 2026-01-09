using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly BookingDbContext _db;
    private readonly BookingService _bookingService;

    public BookingsController(BookingDbContext db, BookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    /// <summary>
    /// Books a single room for the entire inclusive date range [from..to].
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var result = await _bookingService.CreateBookingAsync(request, ct);

        return result switch
        {
            BookingService.BookingSuccess ok =>
                CreatedAtAction(nameof(GetByReference), new { reference = ok.Result.BookingReference }, ok.Result),

            BookingService.BookingFailure f when f.Code == "not_found" =>
                NotFound(new ProblemDetails { Title = "Not found", Detail = f.Message, Status = 404 }),

            BookingService.BookingFailure f when f.Code == "conflict" =>
                Conflict(new ProblemDetails { Title = "Booking conflict", Detail = f.Message, Status = 409 }),

            BookingService.BookingFailure f =>
                BadRequest(new ProblemDetails { Title = "Invalid request", Detail = f.Message, Status = 400 }),

            _ => StatusCode(500, new ProblemDetails { Title = "Unexpected error", Status = 500 })
        };
    }


    [HttpGet("{reference}")]
    [ProducesResponseType(typeof(BookingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDetailsDto>> GetByReference(
    [FromRoute] string reference,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Booking not found",
                Detail = "Booking reference was not provided.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var normalized = reference.Trim();

        // Query only raw fields that translate cleanly to SQL.
        var raw = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.BookingReference == normalized)
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
                    RoomId = r.Id,
                    r.RoomNumber,
                    r.RoomType,
                    r.Capacity,
                    bh.b.StartDate,
                    bh.b.EndDate,
                    Guests = bh.b.GuestCount,
                    bh.b.CreatedUtc
                })
            .FirstOrDefaultAsync(ct);

        if (raw is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Booking not found",
                Detail = $"No booking exists with reference '{normalized}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Map + format in-memory (safe across providers).
        var dto = new BookingDetailsDto(
            raw.BookingReference,
            raw.HotelId,
            raw.HotelName,
            raw.RoomId,
            raw.RoomNumber,
            raw.RoomType,
            raw.Capacity,
            raw.StartDate.ToString("yyyy-MM-dd"),
            raw.EndDate.ToString("yyyy-MM-dd"),
            raw.Guests,
            raw.CreatedUtc.ToString("O")
        );

        return Ok(dto);
    }


}
