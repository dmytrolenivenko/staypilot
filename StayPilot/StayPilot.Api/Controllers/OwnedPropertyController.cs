using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
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
        [HttpPost]
        public async Task<ActionResult<OwnedPropertyAnalysisResponse>> EstimateEvaluationsOwnedpropertyAsync(int id, int months, int radiusMeters = 2000)
        {
            var result = await _ownedPropertyService.EstimateOwnedPropertyValue(id, radiusMeters, months);

            return this.ToActionResult(result);
        }
    }
}
