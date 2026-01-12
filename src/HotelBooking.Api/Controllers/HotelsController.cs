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

    /// <summary>
    /// Creates a controller for hotel search operations.
    /// </summary>
    /// <param name="db">Database context.</param>
    public HotelsController(BookingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Searches hotels by name using a case-insensitive partial match.
    /// </summary>
    /// <param name="name">Hotel name (or part of it).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A search result containing matching hotels and a contextual message.</returns>
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
