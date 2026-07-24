
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    public class OwnedPropertyService : IOwnedPropertyService
    {
        private readonly IOwnedPropertyRepository _ownedPropertyRepository;
        private readonly IMarketAreaRepository _marketAreaRepository;
        private readonly IBeachMarkerRepository _beachMarkerRepository;
        private readonly IPropertyListingRepository _propertyListingRepository;

        public OwnedPropertyService(IOwnedPropertyRepository ownedPropertyRepository, IMarketAreaRepository marketAreaRepository, IBeachMarkerRepository beachMarkerRepository, IPropertyListingRepository propertyListingRepository)
        {
            _ownedPropertyRepository = ownedPropertyRepository;
            _marketAreaRepository = marketAreaRepository;
            _beachMarkerRepository = beachMarkerRepository;
            _propertyListingRepository = propertyListingRepository;
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

        public async Task<OwnedPropertyAnalysisResponse?> EstimateOwnedPropertyValue(int id, int months)
        {
            var ownedPropertyRepo = await _ownedPropertyRepository.GetOwnedPropertyAsync(id);

            if (ownedPropertyRepo is null)
            {
                return null;
            }

            var ownedProperty = Converter.MapToResponse(ownedPropertyRepo);

            var similarPropertiesRepo = await _propertyListingRepository.GetComparablePropertyListingAsync(ownedProperty.MarketAreaId, ownedProperty.PropertyType,  ownedProperty.Typology, ownedProperty.AreaM2, months);

            // No comparable listings -> we cannot estimate anything. Return an empty,
            // low-confidence result instead of indexing into empty lists below (which
            // would throw ArgumentOutOfRangeException).
            if (similarPropertiesRepo.Count == 0)
            {
                return new OwnedPropertyAnalysisResponse
                {
                    CompsCount = 0,
                    ConfidenceLevel = ValuationConfidence.Low
                };
            }

            var sortedPricesPerM2 = similarPropertiesRepo.OrderBy(x => x.ListingSnapshots.First().PricePerM2).Select(x => x.ListingSnapshots.First().PricePerM2).ToList();

            // listings prices
            var sortedPrices = similarPropertiesRepo.OrderBy(x => x.ListingSnapshots.First().Price).Select(x => x.ListingSnapshots.First().Price).ToList();
            var minListingPrice = sortedPrices[0];
            var medianListingPrice = GetMedianValue(sortedPrices);
            var maxListingPrice = sortedPrices[^1];

            // features price incriments
            //...
            //...

            // owned property precies per m2
            var priceBeforeAdjustments = GetMedianValue(sortedPricesPerM2) * ownedProperty.AreaM2;

            var minOwnedPropertyPrice = sortedPricesPerM2[0] * ownedProperty.AreaM2; // + features
            var medianOwnedPropertyPrice = GetMedianValue(sortedPricesPerM2) * ownedProperty.AreaM2; // + features
            var maxOwnedPropertyPrice = sortedPricesPerM2[^1] * ownedProperty.AreaM2; // + features

            // comps comparation
            var minCompsPricePerM2 = sortedPricesPerM2[0];
            var medianCompsPricePerM2 = GetMedianValue(sortedPricesPerM2);
            var maxCompsPricePerM2 = sortedPricesPerM2[^1];

            var compsCount = similarPropertiesRepo.Count;

            var confidenceLevel = compsCount < 3
                ? ValuationConfidence.Low
                : (compsCount < 5 ? ValuationConfidence.Medium : ValuationConfidence.High);

            var finalEstimate = new OwnedPropertyAnalysisResponse
            {
                MinPrice = minOwnedPropertyPrice,
                MidPrice = medianOwnedPropertyPrice,
                MaxPrice = maxOwnedPropertyPrice,

                ConfidenceLevel = confidenceLevel,
                CompsCount = compsCount,

                MarketRatePerM2 = medianCompsPricePerM2,
                EstimateBeforeAdjustments = priceBeforeAdjustments,

                MinCompPricePerM2 = minCompsPricePerM2,
                MedianCompPricePerM2 = medianCompsPricePerM2,
                MaxCompPricePerM2 = maxCompsPricePerM2,

                Adjustments = similarPropertiesRepo.Select(x => new ValuationAdjustment
                {
                    Label = "balcony",
                    Amount = 123,
                }).ToList(),

                Comps = similarPropertiesRepo.Select(x => new ValuationComp
                {
                    AreaM2 = x.AreaM2,
                    PricePerM2 = x.ListingSnapshots.First().PricePerM2,
                    DistanceToBeachMeters = x.DistanceToBeachMeters,
                    Typology = x.Typology,
                    SnapshotDateUtc = x.ListingSnapshots.First().SnapshotDateUtc,
                }).ToList(),
            };

            return finalEstimate;
        }

        private decimal GetMedianValue(List<decimal> list)
        {
            var count = list.Count;
            return count %2 != 0 ? list[count / 2] : (list[(count / 2)] + list[(count / 2) - 1]) / 2;
        }

    }
}
