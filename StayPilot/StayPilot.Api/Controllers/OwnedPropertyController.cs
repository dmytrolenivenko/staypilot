using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{id}")]
        public async Task<ActionResult<OwnedPropertyResponse>> GetOwnedPropertyAsync(int id)
        {
            var result = await _ownedPropertyService.GetOwnedPropertyAsync(id);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<OwnedPropertyResponse>> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var result = await _ownedPropertyService.AddOwnedPropertyAsync(request);

            return CreatedAtAction(nameof(GetOwnedPropertyAsync), new { id = result.Id }, result);
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

    }
}
