using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoint for the investment analysis of a single property listing: renovation cost,
    /// resale value, profit — and, once wired in, an AI-written thesis grounded in those numbers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class InvestmentAnalysisController : ControllerBase
    {
        private readonly IInvestmentAnalysisService _service;

        public InvestmentAnalysisController(IInvestmentAnalysisService service)
        {
            _service = service;
        }

        /// <summary>
        /// Analyzes one property listing by its Id.
        /// Returns 404 if the listing does not exist, 400 if its town has no move-in-ready
        /// median to resell against, or if <paramref name="renovationCostOverride"/> is negative.
        /// </summary>
        /// <param name="propertyListingId">The listing to analyze.</param>
        /// <param name="renovationCostOverride">
        /// Optional. When set, replaces the calculated renovation cost — real repair costs vary
        /// too much (self-sourced materials, no labor hired) for one build-rate formula to fit
        /// everyone.
        /// </param>
        /// <param name="cancellationToken"></param>
        [HttpGet("{propertyListingId}")]
        public async Task<ActionResult<InvestmentAnalysisResponse>> Analyze(int propertyListingId, [FromQuery] decimal? renovationCostOverride, CancellationToken cancellationToken)
        {
            var result = await _service.AnalyzeAsync(propertyListingId, renovationCostOverride, cancellationToken);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Analyzes one owned property by its Id — same math as <see cref="Analyze"/>, with the
        /// purchase price standing in for the ask price.
        /// Returns 404 if the property does not exist, 400 if its town has no move-in-ready
        /// median to resell against, or if <paramref name="renovationCostOverride"/> is negative.
        /// </summary>
        [HttpGet("{ownedPropertyId}")]
        public async Task<ActionResult<InvestmentAnalysisResponse>> AnalyzeOwnedProperty(int ownedPropertyId, [FromQuery] decimal? renovationCostOverride, CancellationToken cancellationToken)
        {
            var result = await _service.AnalyzeOwnedPropertyAsync(ownedPropertyId, renovationCostOverride, cancellationToken);

            return this.ToActionResult(result);
        }
    }
}
