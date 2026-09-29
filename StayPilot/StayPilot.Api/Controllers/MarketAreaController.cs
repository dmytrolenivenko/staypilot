using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
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
        private readonly IStatsRecalculationRunner _recalculationRunner;

        public MarketAreaController(
            IMarketAreaService service,
            IMarketAreaStatsService statsService,
            IStatsRecalculationRunner recalculationRunner)
        {
            _service = service;
            _statsService = statsService;
            _recalculationRunner = recalculationRunner;
        }

        /// <summary>
        /// Return one page of market areas, plus the total number of matches.
        /// Optional search text narrows the list by district, municipality, town or zone.
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = OutputCachePolicies.PublicMarketData)]
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
        [OutputCache(PolicyName = OutputCachePolicies.PublicMarketData)]
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
        [OutputCache(PolicyName = OutputCachePolicies.PublicMarketData)]
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
        [OutputCache(PolicyName = OutputCachePolicies.PublicMarketData)]
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
        [OutputCache(PolicyName = OutputCachePolicies.PublicMarketData)]
        public async Task<ActionResult<MarketAreaNeighbourGapResponse>> GetNeighbourGaps([FromQuery] MarketAreaNeighbourGapRequest request)
        {
            var response = await _statsService.GetNeighbourGapsAsync(request);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Start working the price numbers out again from every listing we hold, replacing the
        /// whole stats table. Run it after importing listings.
        ///
        /// Answers as soon as the work has started, not when it has finished: at national size
        /// the run takes minutes and Azure closes the request at around 230 seconds with a 504,
        /// so waiting for it here meant it could never report success. Poll
        /// <see cref="GetRecalculationStatus"/> to find out how it went. Asking again while a run
        /// is going changes nothing - it reports the run already in progress rather than starting
        /// a second one.
        /// </summary>
        [Authorize]
        [HttpPost]
        public ActionResult<RecalculateMarketAreaStatsResponse> RecalculateMarketAreaStats()
        {
            var response = _recalculationRunner.Start();

            return this.ToActionResult(response);
        }

        /// <summary>
        /// How the current - or most recent - recalculation is doing. Forgotten when the API
        /// restarts; the durable answer to "are these numbers fresh" is the CalculatedAtUtc that
        /// comes back with the leaderboard.
        /// </summary>
        [Authorize]
        [HttpGet]
        public ActionResult<RecalculateMarketAreaStatsResponse> GetRecalculationStatus()
        {
            var response = _recalculationRunner.GetStatus();

            return this.ToActionResult(response);
        }
    }
}
