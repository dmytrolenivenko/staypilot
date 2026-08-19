using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// What it costs to build a house, priced from live public data rather than a stored list.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class BuildCostController : ControllerBase
    {
        private readonly IBuildCostService _service;

        public BuildCostController(IBuildCostService service)
        {
            _service = service;
        }

        /// <summary>
        /// Return every build rate, escalated to the latest month INE has published.
        ///
        /// Nothing here is stored. Each figure is a 2021 anchor carried forward by INE's
        /// construction cost index, so the numbers move on their own when INE publishes.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BuildCostBasisResponse>> GetBasis(CancellationToken cancellationToken)
        {
            var response = await _service.GetBuildCostBasisAsync(cancellationToken);

            return this.ToActionResult(response);
        }
    }
}
