using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;

namespace StayPilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListPropertyListingController : ControllerBase
    {
        private readonly IPropertyListingService _service;

        public ListPropertyListingController(IPropertyListingService service)
        {
            _service = service;
        }

        // Controller to filter Properties
        [HttpPost]
        public async Task<ActionResult<ListPropertyListingResponse>> FilterPropertyAsync(ListPropertyListingRequest request)
        {

            var result = await _service.FilterPropertyAsync(request);

            return Ok(result);
        }
    }
}
