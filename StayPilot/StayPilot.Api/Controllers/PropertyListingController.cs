using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers

{
    /// <summary>
    /// Endpoints for a single property.
    /// It can save a new property and read one property by its Id.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class PropertyListingController : ControllerBase
    {
        private readonly IPropertyListingService _service;

        public PropertyListingController(IPropertyListingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save a new property.
        /// Returns the saved property and a link to read it by its Id.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PropertyListingResponse>> AddPropertyListing(PropertyListingRequest request)
        {
            var result = await _service.AddPropertyListingAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Return one property by its Id.
        /// Returns 404 Not Found if no property has this Id.
        /// </summary>
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

        /// <summary>
        /// Search properties by the given filters.
        /// Returns one page of properties and the total count of matches.
        /// </summary>

        [HttpPost]
        public async Task<ActionResult<FilterPropertyListingResponse>> FilterPropertyAsync(FilterPropertyListingRequest request)
        {
            var result = await _service.FilterPropertyListingAsync(request);

            return Ok(result);
        }
    }
}