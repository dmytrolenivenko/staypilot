using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces;

namespace StayPilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketAreaController : ControllerBase
    {
        private readonly IMarketAreaService _service;

        public MarketAreaController(IMarketAreaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<MarketAreaResponse>>> GetAll()
        {
            var response = await _service.GetAllMarketAreasAsync();
            return Ok(response);
        }
    }
}
