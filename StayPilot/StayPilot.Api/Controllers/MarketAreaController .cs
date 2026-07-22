using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoints for market areas.
    /// A market area is a place (country, district, town, zone) used to group properties.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MarketAreaController : ControllerBase
    {
        private readonly IMarketAreaService _service;

        public MarketAreaController(IMarketAreaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Return all market areas.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<MarketAreaResponse>>> GetAll()
        {
            var response = await _service.GetAllMarketAreasAsync();
            return Ok(response);
        }

        /// <summary>
        /// Return the list of choices for one address level (like town names).
        /// The parts you send narrow the list. Example: send a district to get its towns.
        /// </summary>
        [HttpGet("options")]
        public async Task<ActionResult<List<string>>> GetOptions([FromQuery] string? district, [FromQuery] string? municipality, [FromQuery] string? town)
        {
            var options = await _service.GetMarketAreaOptionsAsync(district, municipality, town);
            return Ok(options);
        }
    }
}
