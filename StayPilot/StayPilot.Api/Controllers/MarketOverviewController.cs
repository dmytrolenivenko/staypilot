using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoint for the market overview: what one place is asking, for one kind of property.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class MarketOverviewController : ControllerBase
    {
        private readonly IMarketOverviewService _service;

        public MarketOverviewController(IMarketOverviewService service)
        {
            _service = service;
        }

        /// <summary>
        /// Return the price numbers for one slice of the market: how many listings there are, then
        /// the price, the price for each square meter and the floor area each as a middle value, an
        /// average, a lowest and a highest, plus the price distribution and a row per room layout.
        ///
        /// Worked out from the listings on every call, so any place crossed with any property type
        /// and layout can be asked for - no recalculation step to run first. A slice with no
        /// listings answers 200 with a count of zero: an empty market is an answer, not an error.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<MarketOverviewResponse>> GetMarketOverview([FromQuery] MarketOverviewRequest request)
        {
            var response = await _service.GetMarketOverviewAsync(request);

            return this.ToActionResult(response);
        }
    }
}
