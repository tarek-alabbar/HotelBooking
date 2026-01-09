using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService)
    {
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

    // Stub for now; we implement it properly next.
    [HttpGet("{reference}")]
    public IActionResult GetByReference([FromRoute] string reference)
        => NotFound(new ProblemDetails
        {
            Title = "Not implemented yet",
            Detail = "Booking lookup will be implemented next.",
            Status = StatusCodes.Status404NotFound
        });
}
