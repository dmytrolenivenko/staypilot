using JetBrains.Annotations;
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// One listing, cut down to what the market overview measures: its newest asking price, its
    /// area, and the place it sits in. A read model for that query, not an entity - nothing stores
    /// it. It exists so the query can select exactly these columns instead of hydrating a full
    /// listing/snapshot/market-area graph for every row in the slice, which is real, avoidable
    /// cost once the slice runs to tens of thousands of listings.
    /// </summary>
    public readonly record struct MarketOverviewListingRow(
        decimal Price,
        decimal PricePerM2,
        int AreaM2,
        Typology Typology,
        string District,
        string Municipality,
        string Town);

    /// <summary>
    /// One listing, cut down to what the market area stats roll-up measures: its newest price,
    /// its place, and the handful of fields that decide its renovation split and its centroid. A
    /// read model for that query, not an entity - nothing stores it. Built for the same reason as
    /// <see cref="MarketOverviewListingRow"/>: the recalculation walks every listing in the
    /// database, and hydrating a full listing/snapshot/market-area graph for each one is real,
    /// avoidable cost this admin action pays for nothing.
    /// </summary>
    public readonly record struct MarketAreaStatsListingRow(
        decimal Price,
        decimal PricePerM2,
        int AreaM2,
        Typology Typology,
        PropertyCondition Condition,
        string? EnergyCertificate,
        decimal Latitude,
        decimal Longitude,
        string District,
        string Municipality,
        string Town);

    /// <summary>
    /// Reads and saves property listings in the database.
    /// </summary>
    public interface IPropertyListingRepository
    {
        /// <summary>
        /// Get one property by its Id. Returns null if it does not exist.
        /// </summary>
        Task<PropertyListing?> GetPropertyListingByIdAsync(int id);

        /// <summary>
        /// Get one property by its source URL. Used to check if it is already saved.
        /// Returns null if it does not exist.
        /// </summary>
        Task<List<PropertyListing>?> GetBulkPropertyListingByUrlAsync(List<string> urls);


        /// <summary>
        /// Add a new property. Call SaveChangesAsync to commit it.
        /// </summary>
        Task<PropertyListing> AddPropertyListingAsync(PropertyListing propertyListing);

        /// <summary>
        /// Search properties with filters, one page at a time.
        /// Returns the page of items and the total number of matches.
        /// </summary>
        Task<(List<PropertyListing> Items, int TotalRecords)> FilterPropertyAsync(FilterPropertyListingRequest request);

        /// <summary>
        /// Write all pending changes to the database.
        /// </summary>
        Task SaveChangesAsync();

        /// <summary>
        /// Forget the rows that are queued to be inserted but have not been saved.
        /// Call it after a failed save, or the same rows are sent again with the next one and
        /// fail again.
        /// </summary>
        void DiscardPendingChanges();

        /// <summary>
        /// Finds properties comparable to a given one: same property type, a room layout within
        /// one step, a floor area within a quarter either way, and within radiusMeters of the
        /// given lat/lon. Only counts a listing if its newest snapshot is no older than the
        /// cutoff. Every match is returned, not a top slice - ordered same market area first,
        /// then nearest.
        /// </summary>
        Task<List<PropertyListing>> GetComparablePropertyListingAsync(int marketId, PropertyType propertyType, Typology typology, int areaM2, int? distanceToBeachMeters, decimal latitude, decimal longitude, int radiusMeters, int months);

        /// <summary>
        /// Gets every property listing across the whole dataset, with just its newest
        /// snapshot loaded. Used to calculate feature price premiums, which reads nearly every
        /// scalar column a listing has — no market area filter and no pagination, this is a bulk
        /// read for analysis, not a UI-facing list.
        /// </summary>
        Task<List<PropertyListing>> GetAllListingsForFeaturePremiumCalculationAsync();

        /// <summary>
        /// Gets every property listing across the whole dataset, cut down to what the market area
        /// stats roll-up measures. Same bulk read as <see cref="GetAllListingsForFeaturePremiumCalculationAsync"/>,
        /// projected instead of hydrated: the stats roll-up needs a handful of fields, not the
        /// whole listing.
        /// </summary>
        Task<List<MarketAreaStatsListingRow>> GetAllListingsForMarketAreaStatsAsync();

        /// <summary>
        /// Gets every listing in one slice of the market - a place, optionally narrowed to one
        /// property type and one room layout - cut down to its newest asking price and its place.
        ///
        /// Not paged: the market overview takes medians and a distribution over the whole slice,
        /// and a page of twenty would summarise the page rather than the market. Pass null or an
        /// empty string for any filter you do not want applied.
        /// </summary>
        Task<List<MarketOverviewListingRow>> GetListingsForMarketOverviewAsync(string? district, string? municipality, string? town, PropertyType? propertyType, Typology? typology);

        /// <summary>
        /// Gets every listing in one place with its WHOLE snapshot history loaded, newest first.
        ///
        /// The overview read only ever loads the newest snapshot, which is enough to price a
        /// market but says nothing about how it moved. Demand and the local price trend both
        /// need the history: one measures how long a listing sat before its last sighting, the
        /// other needs two points in time to have a slope at all.
        ///
        /// Pass null or an empty string for any level you do not want applied.
        /// </summary>
        Task<List<PropertyListing>> GetListingsWithHistoryAsync(string? district, string? municipality, string? town);
    }
}
