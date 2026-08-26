
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Application.Services
{
    /// <inheritdoc cref="IOwnedPropertyService"/>
    public class OwnedPropertyService : IOwnedPropertyService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IBeachMarkerRepository _beachMarkerRepository;

        public OwnedPropertyService(
            IOwnedPropertyRepository ownedPropertyRepository,
            IMarketAreaRepository marketAreaRepository,
            IBeachMarkerRepository beachMarkerRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();
            var beackMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            var ownedPropertyEntity = Converter.MapToEntity(request);

            var marketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            // No market area for this address -> we cannot place the property, so we save nothing.
            if (marketAreaId is null)
            {
                var noMarketArea = new OwnedPropertyResponse();
                noMarketArea.AddError(ErrorCode.MarketAreaNotFound, Calculator.DescribeAddress(request.Country, request.District, request.Municipality, request.Town, request.Zone));

                return noMarketArea;
            }

            // GetMarketId only ever gives back an Id from the list we just passed it.
            ownedPropertyEntity.MarketAreaId = marketAreaId.Value;
            ownedPropertyEntity.MarketArea = marketAreaRepo.First(x => x.Id == marketAreaId.Value);

            var closestBeach = Calculator.GetTheClosestBeach(beackMarkerRepo, request.Latitude, request.Longitude);

            // We only fill in beach info when we actually found one - not every property
            // has a beach nearby, which is a normal case, not an error.
            if (closestBeach is not null)
            {
                ownedPropertyEntity.NearestBeachMarker = closestBeach;
                ownedPropertyEntity.NearestBeachName = closestBeach.Name;

                ownedPropertyEntity.DistanceToBeachMeters = (int)Math.Round(Calculator.CalculateDistanceMeters(
                    (double)closestBeach.Latitude, (double)closestBeach.Longitude,
                    (double)request.Latitude, (double)request.Longitude));
            }

            await _ownedPropertyRepository.CreateOwnedPropertyAsync(ownedPropertyEntity);
            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(ownedPropertyEntity);
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyResponse> GetOwnedPropertyAsync(int id)
        {
            // Reads the row by Id. There may not be one.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
            {
                var notFound = new OwnedPropertyResponse();
                notFound.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return notFound;
            }

            return Converter.MapToResponse(entity);
        }

        /// <inheritdoc/>
        public async Task<DeleteOwnedPropertyResponse> DeleteOwnedPropertyAsync(int id)
        {
            var response = new DeleteOwnedPropertyResponse { Id = id };

            // The repository already checks if the row exists (it returns null if not).
            var deletedName = await _ownedPropertyRepository.DeleteOwnedPropertyAsync(id);

            if (deletedName is null)
            {
                response.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return response;
            }

            // Fix: Remove() (inside the repository) only stages the delete.
            // We still need to save, or nothing happens in the database.
            await _ownedPropertyRepository.SaveChangesAsync();

            response.Name = deletedName;

            return response;
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyResponse> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            // Fix: this used to build a brand new entity from the request, so any
            // field the caller did not send would overwrite the saved value with
            // blank/default. Now we load the real row and only change what was sent.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
            {
                var notFound = new OwnedPropertyResponse();
                notFound.AddError(ErrorCode.OwnedPropertyNotFound, id.ToString());

                return notFound;
            }

            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();

            // Same as Add: the address parts decide the market area. Without this, an edit
            // kept whatever location the property was created with, however the user
            // changed the District/Municipality/Town/Zone pickers.
            var marketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            // Asked before anything is copied onto the entity, so a bad address changes nothing.
            if (marketAreaId is null)
            {
                var noMarketArea = new OwnedPropertyResponse();
                noMarketArea.AddError(ErrorCode.MarketAreaNotFound, Calculator.DescribeAddress(request.Country, request.District, request.Municipality, request.Town, request.Zone));

                return noMarketArea;
            }

            Converter.ApplyUpdates(entity, request);

            entity.MarketAreaId = marketAreaId.Value;

            var beachMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            // Read the coordinates off the entity, not the request: an update that leaves
            // them out still recomputes the beach from the ones already saved.
            var closestBeach = Calculator.GetTheClosestBeach(beachMarkerRepo, entity.Latitude, entity.Longitude);

            if (closestBeach is not null)
            {
                entity.NearestBeachMarker = closestBeach;
                entity.NearestBeachName = closestBeach.Name;

                entity.DistanceToBeachMeters = (int)Math.Round(Calculator.CalculateDistanceMeters(
                    (double)closestBeach.Latitude, (double)closestBeach.Longitude,
                    (double)entity.Latitude, (double)entity.Longitude));
            }

            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(entity);
        }

        /// <inheritdoc/>
        public async Task<OwnedPropertyListResponse> GetAllOwnedPropertiesAsync()
        {
            var domainOwnedProperties = await _ownedPropertyRepository.GetAllOwnedPropertyAsync();

            // Reuse the same mapper every other method here uses
            return new OwnedPropertyListResponse
            {
                Items = domainOwnedProperties
                    .Select(x => Converter.MapToResponse(x))
                    .ToList()
            };
        }
    }
}
