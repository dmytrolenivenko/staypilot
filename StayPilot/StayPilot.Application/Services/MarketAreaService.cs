using System.Globalization;
using StayPilot.Application.Contracts.Request;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Application.Helpers.Mappers;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Entities;

namespace StayPilot.Application.Services
{
    /// <summary>
    /// Handles market areas (regions). Reads them from the repository and
    /// turns them into the shape we send back.
    /// </summary>
    public class MarketAreaService : IMarketAreaService
    {
        private readonly IMarketAreaRepository _marketAreaRepo;
        public MarketAreaService(IMarketAreaRepository marketAreaRepo)
        {
            _marketAreaRepo = marketAreaRepo;
        }

        /// <inheritdoc/>
        public async Task<MarketAreaListResponse> GetMarketAreasPageAsync(MarketAreaRequest request)
        {
            // Ask the database for this page of market areas and the total number of matches.
            var (items, totalRecords) = await _marketAreaRepo.GetMarketAreasPageAsync(request);

            // Turn each market area entity into the response we send back,
            // then add the paging info so the caller knows how many pages exist.
            return new MarketAreaListResponse
            {
                Items = items.Select(Converter.MapToResponse).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords
            };
        }

        /// <inheritdoc/>
        public async Task<MarketAreaOptionsResponse> GetMarketAreaOptionsAsync(string? district, string? municipality, string? town)
        {
            var options = await _marketAreaRepo.GetMarketAreaOptionsAsync(district, municipality, town);

            return new MarketAreaOptionsResponse { Items = options };
        }

        /// <inheritdoc/>
        public async Task<MarketAreaTreeResponse> GetMarketAreaTreeAsync()
        {
            var areas = await _marketAreaRepo.GetMarketAreaNamesAsync();

            return new MarketAreaTreeResponse { Districts = BuildTree(areas) };
        }

        /// <summary>
        /// Folds the flat market area rows into district > municipality > town > zones.
        /// Names group and sort the way the database's own picker did: case-insensitive, and in
        /// Portuguese order, so "Évora" sits with the E's instead of after the Z's.
        /// </summary>
        public static List<MarketAreaTreeDistrictResponse> BuildTree(List<MarketArea> areas)
        {
            var names = StringComparer.Create(CultureInfo.GetCultureInfo("pt-PT"), ignoreCase: true);

            return areas
                .GroupBy(x => x.District, names)
                .OrderBy(x => x.Key, names)
                .Select(district => new MarketAreaTreeDistrictResponse
                {
                    Name = district.Key,
                    Municipalities = district
                        .GroupBy(x => x.Municipality, names)
                        .OrderBy(x => x.Key, names)
                        .Select(municipality => new MarketAreaTreeMunicipalityResponse
                        {
                            Name = municipality.Key,
                            Towns = municipality
                                .GroupBy(x => x.Town, names)
                                .OrderBy(x => x.Key, names)
                                .Select(town => new MarketAreaTreeTownResponse
                                {
                                    Name = town.Key,
                                    // A town row with no zone is the town itself, not a zone.
                                    Zones = town
                                        .Where(x => !string.IsNullOrWhiteSpace(x.Zone))
                                        .Select(x => x.Zone!)
                                        .Distinct(names)
                                        .OrderBy(x => x, names)
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();
        }
    }
}
