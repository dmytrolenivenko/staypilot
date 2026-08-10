using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Interfaces.Services;
using JetBrains.Annotations;

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

        [HttpGet("{id}")]
        public async Task<ActionResult<OwnedPropertyResponse>> GetOwnedPropertyAsync(int id)
        {
            var result = await _ownedPropertyService.GetOwnedPropertyAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<OwnedPropertyResponse>> GetAllOwnedPropertyAsync()
        {
            var result = await _ownedPropertyService.GetAllOwnedPropertiesAsync();

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<OwnedPropertyResponse>> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var result = await _ownedPropertyService.AddOwnedPropertyAsync(request);

            // Action name without the "Async" suffix - that is what routing registered,
            // so nameof(GetOwnedPropertyAsync) would not match any route here.
            return CreatedAtAction("GetOwnedProperty", new { id = result.Id }, result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<string>> DeleteOwnedPropertyAsync(int id)
        {
            var result = await _ownedPropertyService.DeleteOwnedPropertyAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OwnedPropertyResponse>> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            var result = await _ownedPropertyService.UpdateOwnedPropertyAsync(id, request);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        // radiusMeters has a default because a missing query string value would otherwise
        // bind to 0, which silently shrinks the search circle to nothing.
        [HttpPost]
        public async Task<ActionResult<OwnedPropertyAnalysisResponse>> EstimateEvaluationsOwnedpropertyAsync(int id, int months, int radiusMeters = 2000)
        {
            var result = await _ownedPropertyService.EstimateOwnedPropertyValue(id, radiusMeters, months);

            return Ok(result);
        }

    }
}
