using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class OwnedPropertyController : ControllerBase
    {
        private readonly IOwnedPropertyService _ownedPropertyService;

        public OwnedPropertyController(IOwnedPropertyService ownedPropertyService)
        {
            _ownedPropertyService = ownedPropertyService;
        }

        /// <summary>
        /// Return one owned property. 404 Not Found when there is no property with this Id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OwnedPropertyResponse>> GetOwnedPropertyAsync(int id)
        {
            var result = await _ownedPropertyService.GetOwnedPropertyAsync(id);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Return every owned property of the user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<OwnedPropertyListResponse>> GetAllOwnedPropertyAsync()
        {
            var result = await _ownedPropertyService.GetAllOwnedPropertiesAsync();

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Save a new owned property.
        /// 400 Bad Request when its address matches no market area - nothing is saved then.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OwnedPropertyResponse>> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var result = await _ownedPropertyService.AddOwnedPropertyAsync(request);

            if (!result.Succeeded)
            {
                return this.ToActionResult(result);
            }

            // Action name without the "Async" suffix - that is what routing registered,
            // so nameof(GetOwnedPropertyAsync) would not match any route here.
            return CreatedAtAction("GetOwnedProperty", new { id = result.Id }, result);
        }

        /// <summary>
        /// Delete one owned property. 404 Not Found when there is no property with this Id.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<DeleteOwnedPropertyResponse>> DeleteOwnedPropertyAsync(int id)
        {
            var result = await _ownedPropertyService.DeleteOwnedPropertyAsync(id);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Change one owned property. Only the fields sent are touched.
        /// 404 Not Found when there is no such property, 400 Bad Request when the new address
        /// matches no market area - nothing is changed in either case.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<OwnedPropertyResponse>> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            var result = await _ownedPropertyService.UpdateOwnedPropertyAsync(id, request);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Work out what one owned property is worth today.
        /// 404 Not Found when there is no such property, 400 Bad Request when we hold too few
        /// listings to price anything.
        /// </summary>
        // radiusMeters has a default because a missing query string value would otherwise
        // bind to 0, which silently shrinks the search circle to nothing.
        // The bounds mirror what the UI already enforces - months=0 and a negative radius
        // were both accepted before.
        [HttpPost]
        public async Task<ActionResult<OwnedPropertyAnalysisResponse>> EstimateEvaluationsOwnedpropertyAsync(int id, [Range(1, 120)] int months, [Range(100, 20_000)] int radiusMeters = 2000)
        {
            var result = await _ownedPropertyService.EstimateOwnedPropertyValue(id, radiusMeters, months);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Read back the last priced result for every owned property. A plain table read - no
        /// model fit, no comp search - so this is what the Valuation screen loads on every visit.
        /// A property never valued yet comes back as a placeholder row (its price fields at zero)
        /// rather than being left out; an empty list is a normal 200 for a user who owns nothing.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<OwnedPropertyPortfolioResponse>> ListValuationsOwnedpropertyAsync()
        {
            var result = await _ownedPropertyService.GetCachedPortfolioAsync();

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Price every owned property at once, each with how keen buyers are in its place and
        /// where its value is heading, and save the result - this is the expensive path, run
        /// only when the user presses "Re-price". 400 Bad Request when we hold too few listings
        /// to price anything; an empty list is a normal 200 for a user who owns nothing yet.
        /// </summary>
        // Every parameter defaults, because a missing query string value binds to 0 - which
        // would shrink the comparable search to nothing and project zero years forward.
        // The bounds mirror what the UI already enforces. Without them years=999 ran the
        // compounding loop until decimal overflowed and answered 500 after half a minute.
        //
        // Just the class-level [Authorize], not Api.Write - unlike RecalculateMarketAreaStats
        // and ReCalculatePremiumFeaturesValue, this only ever touches the caller's own
        // properties (OwnedPropertyService resolves ownerUserId from ICurrentUser), so a
        // tenant recalculating their own portfolio is fine.
        [HttpPost]
        public async Task<ActionResult<OwnedPropertyPortfolioResponse>> RevalueOwnedPropertiesAsync([Range(1, 120)] int months = 12, [Range(100, 20_000)] int radiusMeters = 2000, [Range(1, 30)] int years = 10)
        {
            var result = await _ownedPropertyService.RevalueOwnedPropertiesAsync(radiusMeters, months, years);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Prices one owned property and overwrites its stored valuation.
        /// 404 Not Found when there is no such property, 400 Bad Request when we hold too few
        /// listings to price anything.
        /// </summary>
        [HttpPost("{id}")]
        public async Task<ActionResult<OwnedPropertyValuationResponse>> RevalueOwnedPropertyAsync(int id, [Range(1, 120)] int months = 12, [Range(100, 20_000)] int radiusMeters = 2000, [Range(1, 30)] int years = 10)
        {
            var result = await _ownedPropertyService.RevalueOwnedPropertyAsync(id, radiusMeters, months, years);

            return this.ToActionResult(result);
        }
    }
}
