using HotelBooking.Api.Application.Dtos;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Api.Controllers;

[ApiController]
[Route("api/hotels")]
public sealed class HotelsController : ControllerBase
{
    private readonly BookingDbContext _db;

    public HotelsController(BookingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Find hotels by name (case-insensitive, partial match).
    /// </summary>
    /// <param name="name">Hotel name (or part of it).</param>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto<HotelSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<HotelSummaryDto>>> Search([FromQuery] string? name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var problem = new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "Query parameter 'name' is required." }
                })
            {
                Title = "Invalid query parameters",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problem);
        }



        var term = name.Trim();

        // SQLite supports case-insensitive LIKE by default for ASCII,
        // but to be explicit and consistent, we normalize both sides.
        var normalized = term.ToUpperInvariant();

        var hotels = await _db.Hotels
            .AsNoTracking()
            .Where(h => EF.Functions.Like(h.Name.ToUpper(), $"%{normalized}%"))
            .OrderBy(h => h.Name)
            .Select(h => new HotelSummaryDto(h.Id, h.Name))
            .ToListAsync(ct);
        
        var message = hotels.Count == 0
            ? $"No hotels found matching '{term}'."
            : $"Found {hotels.Count} hotel(s).";
        
        return Ok(new SearchResultDto<HotelSummaryDto>(hotels, message));
    }
}
