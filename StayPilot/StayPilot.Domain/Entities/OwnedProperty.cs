using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// A property that the user owns.
    /// It holds the buy price, the size, and all the features of the home.
    /// </summary>
    public class OwnedProperty
    {
        /// <summary>
        /// Database Id for this owned property.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// A short name the user gives to this property.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Id of the market area (the place) this property is in.
        /// </summary>
        public int MarketAreaId { get; set; }

        /// <summary>
        /// The market area (the place) this property is in.
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
        /// Price the user paid to buy this property.
        /// </summary>
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// Date the user bought this property.
        /// </summary>
        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Money spent on repairs or upgrades. Can be empty.
        /// </summary>
        public decimal? RenovationInvestment { get; set; }

        /// <summary>
        /// Floor area in square meters.
        /// </summary>
        public int AreaM2 { get; set; }

        /// <summary>
        /// Number of bathrooms. Can be empty if not known.
        /// </summary>
        public int? Bathrooms { get; set; }

        /// <summary>
        /// Which floor the property is on. Can be empty.
        /// </summary>
        public int? Floor { get; set; }

        /// <summary>
        /// How many floors the building has. Can be empty.
        /// </summary>
        public int? TotalFloors { get; set; }

        /// <summary>
        /// True if the building has a lift. Can be empty if not known.
        /// </summary>
        public bool? HasElevator { get; set; }

        public bool? HasAirConditioning { get; set; }

        /// <summary>
        /// Year the property was built. Can be empty.
        /// </summary>
        public int? ConstructionYear { get; set; }

        /// <summary>
        /// Year of the last big repair. Can be empty.
        /// </summary>
        public int? RenovationYear { get; set; }

        /// <summary>
        /// State of the property (new, used, needs repair...).
        /// </summary>
        public PropertyCondition Condition { get; set; }

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
        public bool HasCityView { get; set; }

        /// <summary>
        /// Walking distance to the nearest beach, in meters. Can be empty.
        /// </summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>
        /// The nearest beach point. Can be empty.
        /// </summary>
        public BeachMarker? NearestBeachMarker { get; set; }

        /// <summary>
        /// Name of the nearest beach. Can be empty.
        /// </summary>
        public string? NearestBeachName { get; set; }

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
        /// When we saved this property (UTC time). Defaults to now.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this property was last changed (UTC time). Empty until first change.
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }
    }
}