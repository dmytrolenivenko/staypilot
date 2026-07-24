
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Handles listing snapshots (price, status, date) for a property.
    /// </summary>
    public class ListingSnapshotService : IListingSnapshotService
    {
        private readonly IListingSnapshotRepository _listingSnapshotRepo;

        public ListingSnapshotService(IListingSnapshotRepository listingSnapshotRepo)
        {
            _listingSnapshotRepo = listingSnapshotRepo;
        }

        /// <inheritdoc/>
        public async Task<ListingSnapshotResponse> CreateListingSnapshotAsync(ListingSnapshotRequest request)
        {
            // Build the entity from the request and save it.
            var snapshot = Helpers.Mappers.Converter.MapToEntity(request);
            await _listingSnapshotRepo.AddListingSnapshotAsync(snapshot);

            // Actually write it to the database. Without this, AddAsync only stages the
            // row in the change tracker and nothing is ever persisted.
            await _listingSnapshotRepo.SaveChangesAsync();

            return Helpers.Mappers.Converter.MapToResponse(snapshot);
        }

        /// <inheritdoc/>
        public async Task<ListingSnapshotResponse> GetListingSnapshotByPropertyIdAsync(int propertyListingId)
        {
            var snapshot = await _listingSnapshotRepo.GetListingSnapshotByPropertyIdAsync(propertyListingId);

            // No snapshot for this property -> tell the caller with an error.
            if (snapshot == null)
            {
                throw new KeyNotFoundException($"Snapshot with Property ID {propertyListingId} not found.");
            }
            return Helpers.Mappers.Converter.MapToResponse(snapshot);
        }

    }
}
