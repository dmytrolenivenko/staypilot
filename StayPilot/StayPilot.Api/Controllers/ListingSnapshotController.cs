using Microsoft.AspNetCore.Mvc;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Controllers
{
    /// <summary>
    /// Endpoints for listing snapshots (the price and state of a listing at one point in time).
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ListingSnapshotController : ControllerBase
    {
        private readonly IListingSnapshotService _service;

        public ListingSnapshotController(IListingSnapshotService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save a new snapshot (price, status, date) for an existing property listing.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ListingSnapshotResponse>> CreateListingSnapshotAsync(ListingSnapshotRequest request)
        {
            var result = await _service.CreateListingSnapshotAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Return the snapshot of one property, by the property's Id.
        /// </summary>
        [HttpGet("{propertyListingId}")]
        public async Task<ActionResult<ListingSnapshotResponse>> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            var result = await _service.GetListingSnapshotByPropertyIdAsync(propertyListingId);

            return Ok(result);
        }
    }
}
