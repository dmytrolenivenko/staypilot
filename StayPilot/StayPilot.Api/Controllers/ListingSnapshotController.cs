using Microsoft.AspNetCore.Mvc;
using StayPilot.Api.Extensions;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

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
        [Authorize(Roles = "Api.Write")]
        [HttpPost]
        public async Task<ActionResult<ListingSnapshotResponse>> CreateListingSnapshotAsync(ListingSnapshotRequest request)
        {
            var result = await _service.CreateListingSnapshotAsync(request);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Return the snapshot of one property, by the property's Id.
        /// Returns 404 Not Found if the property has no snapshot.
        /// </summary>
        [HttpGet("{propertyListingId}")]
        public async Task<ActionResult<ListingSnapshotResponse>> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            var result = await _service.GetListingSnapshotByPropertyIdAsync(propertyListingId);

            return this.ToActionResult(result);
        }

        /// <summary>
        /// Compares ActiveUrls against every listing this API holds as Active, and marks
        /// anything missing from that list as sold (a new Sold snapshot, nothing is deleted).
        /// Meant to run right after a scraper's full sweep of the source site, using the URLs
        /// it actually saw still live.
        /// </summary>
        [Authorize(Roles = "Api.Write")]
        [HttpPost]
        public async Task<ActionResult<ReconcileActiveListingsResponse>> ReconcileActiveListingsAsync(ReconcileActiveListingsRequest request)
        {
            var result = await _service.ReconcileActiveListingsAsync(request);

            return this.ToActionResult(result);
        }
    }
}
