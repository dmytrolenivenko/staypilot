using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

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
        /// Save many properties in one call.
        /// Always answers 200: a bulk upload where some listings were saved and others were
        /// rejected is not a failed request. Read TotalAdded and Errors to see what happened.
        /// </summary>
        [Authorize(Roles = "Api.Write")]
        [HttpPost]
        public async Task<ActionResult<BulkAddPropertyListingResponse>> BulkAddPropertyListing(BulkAddPropertyListingRequest request)
        {
            var result = await _service.BulkAddPropertyListingAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Return one property by its Id.
        /// Returns 404 Not Found if no property has this Id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyListingResponse>> GetById(int id)
        {
            var result = await _service.GetPropertyListingByIdAsync(id);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Search properties by the given filters.
        /// Returns one page of properties and the total count of matches.
        /// </summary>

        [HttpPost]
        public async Task<ActionResult<FilterPropertyListingResponse>> FilterPropertyAsync(FilterPropertyListingRequest request)
        {
            var result = await _service.FilterPropertyListingAsync(request);

            return this.ToActionResult(result);
        }
    }
}
