using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces;

namespace StayPilot.Api.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyListingController : ControllerBase
    {
        private readonly IPropertyListingService _service;

        public PropertyListingController(IPropertyListingService service)
        {
            _service = service;
        }

        // Controller action to add a new property listing
        [HttpPost]
        public async Task<ActionResult<PropertyListingResponse>> AddPropertyListing(PropertyListingRequest request)
        {
            var result = await _service.AddPropertyListingAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // Controller action to get a property listing by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyListingResponse>> GetById(int id)
        {
            var result = await _service.GetPropertyListingByIdAsync(id);

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}