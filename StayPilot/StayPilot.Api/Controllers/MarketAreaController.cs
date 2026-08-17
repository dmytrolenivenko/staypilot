using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoints for market areas.
    /// A market area is a place (country, district, town, zone) used to group properties.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MarketAreaController : ControllerBase
    {
        private readonly IMarketAreaService _service;

        public MarketAreaController(IMarketAreaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Return one page of market areas, plus the total number of matches.
        /// Optional search text narrows the list by district, municipality, town or zone.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MarketAreaListResponse>> GetAll([FromQuery] MarketAreaRequest request)
        {
            var response = await _service.GetMarketAreasPageAsync(request);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Return the list of choices for one address level (like town names).
        /// The parts you send narrow the list. Example: send a district to get its towns.
        /// </summary>
        [HttpGet("options")]
        public async Task<ActionResult<MarketAreaOptionsResponse>> GetOptions([FromQuery] string? district, [FromQuery] string? municipality, [FromQuery] string? town)
        {
            var response = await _service.GetMarketAreaOptionsAsync(district, municipality, town);

            return this.ToActionResult(response);
        }
    }
}
