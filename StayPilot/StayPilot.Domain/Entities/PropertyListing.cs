
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// A property for sale that we found on an outside site (a "comp").
    /// We use these listings to compare prices with the user's own properties.
    /// </summary>
    public class PropertyListing
    {
        /// <summary>
        /// Database Id for this listing.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id of the market area (the place) this listing is in.
        /// </summary>
        public int MarketAreaId {  get; set; }

        /// <summary>
        /// The market area (the place) this listing is in.
        /// </summary>
        public MarketArea MarketArea { get; set; } = null!;

        /// <summary>
        /// Kind of property (apartment, villa, house, land).
        /// </summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>
        /// Number of rooms in the Portuguese T-style (T0, T1, T2...).
        /// </summary>
        public Typology Typology { get; set; }

        /// <summary>
        /// Name of the site we took this listing from (for example "Idealista").
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// Web address of the listing. We use it to avoid saving the same listing twice.
        /// </summary>
        public string SourceUrl { get; set;} = string.Empty;

        /// <summary>
        /// Floor area in square meters.
        /// </summary>
        public int AreaM2 { get; set; }

        /// <summary>
        /// Number of bathrooms.
        /// </summary>
        public int Bathrooms { get; set; }

        /// <summary>
        /// Which floor the property is on. Can be empty.
        /// </summary>
        public int? Floor {  get; set; }

        /// <summary>
        /// How many floors the building has. Can be empty.
        /// </summary>
        public int? TotalFloors { get; set; }

        /// <summary>
        /// True if the building has a lift. Can be empty if not known.
        /// </summary>
        public bool? HasElevator { get; set; }

        /// <summary>
        /// True if it has air conditioning. Can be empty if not known.
        /// </summary>
        public bool? HasAirConditioning { get; set; }

        /// <summary>
        /// State of the property (new, used, needs repair...).
        /// </summary>
        public PropertyCondition Condition { get; set; }

        /// <summary>
        /// Year the property was built. Can be empty.
        /// </summary>
        public int? ConstructionYear { get; set; }

        /// <summary>
        /// Year of the last big repair. Can be empty.
        /// </summary>
        public int? RenovationYear { get; set; }

        /// <summary>
        /// How many balconies the property has.
        /// </summary>
        public int BalconyCount { get; set; }

        /// <summary>
        /// True if it has a terrace.
        /// </summary>
        public bool HasTerrace { get; set; }

        /// <summary>
        /// True if it has a garage.
        /// </summary>
        public bool HasGarage { get; set; }

        /// <summary>
        /// True if it has a parking spot.
        /// </summary>
        public bool HasParking { get; set; }

        /// <summary>
        /// True if it has a swimming pool.
        /// </summary>
        public bool HasSwimmingPool { get; set; }

        /// <summary>
        /// True if it comes with furniture.
        /// </summary>
        public bool IsFurnished { get; set; }

        /// <summary>
        /// True if you can see the sea from the property.
        /// </summary>
        public bool HasSeaView { get; set; }

        /// <summary>
        /// True if you can see the city from the property.
        /// </summary>
        public bool HasCityView {  get; set; }

        /// <summary>
        /// Distance to the nearest beach, in meters. Can be empty.
        /// </summary>
        public int? DistanceToBeachMeters {  get; set; }

        /// <summary>
        /// Id of the nearest beach point. Can be empty.
        /// </summary>
        public int? NearestBeachMarkerId { get; set; }

        /// <summary>
        /// The nearest beach point. Can be empty.
        /// </summary>
        public BeachMarker? NearestBeachMarker { get; set; }

        /// <summary>
        /// Name of the nearest beach. Can be empty.
        /// </summary>
        public string? NearestBeachName { get; set; }

        /// <summary>
        /// How we measured the beach distance (for example "osm_center_point").
        /// </summary>
        public string? DistanceToBeachMethod { get; set; }

        /// <summary>
        /// Location: north-south position. Can be empty.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Location: east-west position. Can be empty.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Energy rating letter (for example "A" or "C"). Can be empty.
        /// </summary>
        public string? EnergyCertificate { get; set; }

        /// <summary>
        /// Free text notes for humans. Can be empty.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When we saved this listing (UTC time).
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// When this listing was last changed (UTC time). Empty until first change.
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// Price history: one snapshot per day we checked the listing.
        /// </summary>
        public List<ListingSnapshot> ListingSnapshots { get; set; } = new();
    }
}
