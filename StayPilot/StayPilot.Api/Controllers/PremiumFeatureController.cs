using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;

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

        [HttpGet]
        public async Task<ActionResult<List<PremiumFeatureResponse>>> GetAllPremiumFeatures()
        {
            var result = await _premiumFeatureService.GetAllPremiumFeatures();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<List<PremiumFeature>>> ReCalculatePremiumFeaturesValue()
        {
            var result = await _premiumFeatureService.ReCalculatePremiumFeaturesValue();

            return Ok(result);
        }
    }
}
