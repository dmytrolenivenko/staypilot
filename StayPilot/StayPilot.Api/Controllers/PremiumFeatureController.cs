using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace StayPilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PremiumFeatureController : ControllerBase
    {
        private readonly IPremiumFeatureService _premiumFeatureService;

        public PremiumFeatureController(IPremiumFeatureService premiumFeatureService)
        {
            _premiumFeatureService = premiumFeatureService;
        }

        /// <summary>
        /// What each feature is worth, as of the last recalculation.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PremiumFeatureListResponse>> GetAllPremiumFeatures()
        {
            var result = await _premiumFeatureService.GetAllPremiumFeatures();

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Measure every feature again from the listings we hold.
        /// Returns 400 Bad Request when there is too little data to measure anything.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PremiumFeatureListResponse>> ReCalculatePremiumFeaturesValue()
        {
            var result = await _premiumFeatureService.ReCalculatePremiumFeaturesValue();

            return this.ToActionResult(result);
        }
    }
}
