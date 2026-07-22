using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoints to search properties.
    /// It takes filters and returns one page of matching properties.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ListPropertyListingController : ControllerBase
    {
        private readonly IPropertyListingService _service;

        public ListPropertyListingController(IPropertyListingService service)
        {
            _service = service;
        }

        /// <summary>
        /// Search properties by the given filters.
        /// Returns one page of properties and the total count of matches.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ListPropertyListingResponse>> FilterPropertyAsync(ListPropertyListingRequest request)
        {

            var result = await _service.FilterPropertyAsync(request);

            return Ok(result);
        }
    }
}
