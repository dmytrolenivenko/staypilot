using Microsoft.AspNetCore.Authorization;
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
        private readonly IMarketAreaStatsService _statsService;

        public MarketAreaController(IMarketAreaService service, IMarketAreaStatsService statsService)
        {
            _service = service;
            _statsService = statsService;
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

        /// <summary>
        /// Return the places ranked by price for each square meter, priciest first by default.
        /// Reads numbers worked out earlier, so it is a plain table read - call
        /// RecalculateMarketAreaStats after an import to refresh them.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MarketAreaLeaderboardResponse>> GetLeaderboard([FromQuery] MarketAreaLeaderboardRequest request)
        {
            var response = await _statsService.GetLeaderboardAsync(request);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Return what a budget buys in each place: the most rooms it reaches and how much space
        /// that usually is. Places where the budget reaches nothing are left out.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MarketAreaBudgetResponse>> GetBudgetRanking([FromQuery] MarketAreaBudgetRequest request)
        {
            var response = await _statsService.GetBudgetRankingAsync(request);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Return pairs of nearby places with a big price gap between them - where moving a few
        /// kilometres changes what a square meter costs.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MarketAreaNeighbourGapResponse>> GetNeighbourGaps([FromQuery] MarketAreaNeighbourGapRequest request)
        {
            var response = await _statsService.GetNeighbourGapsAsync(request);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Work the price numbers out again from every listing we hold, replacing the whole
        /// stats table. Run it after importing listings.
        /// </summary>
        [Authorize(Roles = "Api.Write")]
        [HttpPost]
        public async Task<ActionResult<RecalculateMarketAreaStatsResponse>> RecalculateMarketAreaStats()
        {
            var response = await _statsService.RecalculateMarketAreaStatsAsync();

            return this.ToActionResult(response);
        }
    }
}
