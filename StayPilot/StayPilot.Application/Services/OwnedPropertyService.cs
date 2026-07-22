
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;
using StayPilot.Application.Helpers.Mappers;

namespace StayPilot.Application.Services
{
    public class OwnedPropertyService : IOwnedPropertyService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IBeachMarkerRepository _beachMarkerRepository;

        public OwnedPropertyService(IOwnedPropertyRepository ownedPropertyRepository, IMarketAreaRepository marketAreaRepository, IBeachMarkerRepository beachMarkerRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
        }

        public async Task<OwnedPropertyResponse> AddOwnedPropertyAsync(OwnedPropertyRequest request)
        {
            var marketAreaRepo = await _marketAreaRepository.GetAllMarketAreasAsync();
            var beackMarkerRepo = await _beachMarkerRepository.GetAllBeachMarkersAsync();

            var ownedPropertyEntity = Converter.MapToEntity(request);

            ownedPropertyEntity.MarketAreaId = Calculator.GetMarketId(marketAreaRepo, request.Country, request.District, request.Municipality, request.Town, request.Zone);

            ownedPropertyEntity.MarketArea = marketAreaRepo.FirstOrDefault(x => x.Id == ownedPropertyEntity.MarketAreaId) ?? throw new InvalidOperationException("MarketArea can not be null");

            // Without this check, the (double) cast below would crash with a confusing
            // error instead of a clear message, whenever a request has no location.
            if (request.Latitude is null || request.Longitude is null)
                throw new InvalidOperationException("Latitude and Longitude must be provided for the owned property.");

            var closestBeach = Calculator.GetTheClosestBeach(beackMarkerRepo, request.Latitude, request.Longitude);

            // We only fill in beach info when we actually found one.
            if (closestBeach is not null)
            {
                ownedPropertyEntity.NearestBeachMarker = closestBeach;
                ownedPropertyEntity.NearestBeachName = closestBeach.Name;

                ownedPropertyEntity.DistanceToBeachMeters = (int)Math.Round(Calculator.CalculateDistanceMeters(
                    (double)closestBeach.Latitude, (double)closestBeach.Longitude,
                    (double)request.Latitude.Value, (double)request.Longitude.Value));
            }

            await _ownedPropertyRepository.CreateOwnedPropertyAsync(ownedPropertyEntity);
            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(ownedPropertyEntity);
        }

        public async Task<OwnedPropertyResponse?> GetOwnedPropertyAsync(int id)
        {
            // Now it just reads the row by Id, and returns null when there is no such row.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            return entity is null ? null : Converter.MapToResponse(entity);
        }

        public async Task<string?> DeleteOwnedPropertyAsync(int id)
        {
            // Fix: this used to take a string Id and always throw NotImplementedException.
            // The repository already checks if the row exists (it returns null if not).
            var deletedName = await _ownedPropertyRepository.DeleteOwnedPropertyAsync(id);

            if (deletedName is null)
                return null;

            // Fix: Remove() (inside the repository) only stages the delete.
            // We still need to save, or nothing happens in the database.
            await _ownedPropertyRepository.SaveChangesAsync();

            return deletedName;
        }

        public async Task<OwnedPropertyResponse?> UpdateOwnedPropertyAsync(int id, OwnedPropertyRequest request)
        {
            // Fix: this used to build a brand new entity from the request, so any
            // field the caller did not send would overwrite the saved value with
            // blank/default. Now we load the real row and only change what was sent.
            var entity = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (entity is null)
                return null;

            Converter.ApplyUpdates(entity, request);

            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _ownedPropertyRepository.SaveChangesAsync();

            return Converter.MapToResponse(entity);
        }
    }
}
