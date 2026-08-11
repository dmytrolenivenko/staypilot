using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// A property described in the exact terms the valuation model prices on, and nothing more.
    ///
    /// It exists so the model can treat a scraped comp and one of the user's own apartments as
    /// the same kind of thing. Without it, every part of the model would need two versions -
    /// one reading <see cref="PropertyListing"/> (non-null bools, non-null Condition) and one
    /// reading <see cref="OwnedPropertyResponse"/> (nullable everything) - and the two would
    /// eventually disagree about what "no garage" means.
    /// </summary>
    public class ValuationSubject
    {
        /// <summary>Which market area the property sits in. Drives the location baseline.</summary>
        public int MarketAreaId { get; set; }

        /// <summary>Number of rooms, Portuguese T-style (T0, T1, T2...).</summary>
        public Typology Typology { get; set; }

        /// <summary>
        /// Apartment, villa, house or land. Every listing collected so far is an apartment, so
        /// this currently does nothing - it is here so the model does not silently price a villa
        /// as an apartment the day the scraper starts collecting them.
        /// </summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>State of the property. Unknown is treated as the ordinary "Good" case.</summary>
        public PropertyCondition Condition { get; set; } = PropertyCondition.Good;

        /// <summary>Floor area in square meters. Must be greater than zero to be priceable.</summary>
        public int AreaM2 { get; set; }

        public int Bathrooms { get; set; }

        public int BalconyCount { get; set; }

        /// <summary>Which floor it is on, or null when the source did not say.</summary>
        public int? Floor { get; set; }

        /// <summary>Year built, or null when the source did not say.</summary>
        public int? ConstructionYear { get; set; }

        /// <summary>
        /// The energy certificate as a number so it can be priced per step: G is 0 through to
        /// A+ at 8. Null when the certificate is missing or not a grade we recognise, which the
        /// model flags rather than guessing at.
        /// </summary>
        public int? EnergyGradeScore { get; set; }

        /// <summary>Walking-line distance to the nearest beach, or null when unknown.</summary>
        public int? DistanceToBeachMeters { get; set; }

        /// <summary>Coordinates. Needed for the neighbourhood correction; null just skips it.</summary>
        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public bool HasElevator { get; set; }

        public bool HasTerrace { get; set; }

        public bool HasGarage { get; set; }

        public bool HasSwimmingPool { get; set; }

        public bool IsFurnished { get; set; }

        public bool HasParking { get; set; }

        public bool HasSeaView { get; set; }

        public bool HasCityView { get; set; }

        /// <summary>
        /// Turns an energy certificate letter into a position on the scale, so the model can
        /// price one step rather than nine separate letters. B- sits between B and C, which is
        /// what the Portuguese scale means by it.
        ///
        /// Anything unrecognised comes back null rather than being pushed to one end - a typo
        /// scored as "G" would read as the worst possible rating instead of as missing.
        /// </summary>
        public static int? ScoreEnergyGrade(string? certificate)
        {
            if (string.IsNullOrWhiteSpace(certificate))
                return null;

            return certificate.Trim().ToUpperInvariant() switch
            {
                "A+" => 8,
                "A" => 7,
                "B" => 6,
                "B-" => 5,
                "C" => 4,
                "D" => 3,
                "E" => 2,
                "F" => 1,
                "G" => 0,
                _ => null,
            };
        }

        /// <summary>
        /// Reads a scraped comp. Note HasAirConditioning is deliberately not carried over:
        /// the column has thousands of trues and zero explicit falses, so "false" really means
        /// "the advert did not mention it" - modelling it would price the copywriting.
        /// </summary>
        public static ValuationSubject FromListing(PropertyListing listing)
        {
            return new ValuationSubject
            {
                MarketAreaId = listing.MarketAreaId,
                Typology = listing.Typology,
                PropertyType = listing.PropertyType,
                Condition = listing.Condition,
                AreaM2 = listing.AreaM2,
                Bathrooms = listing.Bathrooms,
                BalconyCount = listing.BalconyCount,
                Floor = listing.Floor,
                ConstructionYear = listing.ConstructionYear,
                EnergyGradeScore = ScoreEnergyGrade(listing.EnergyCertificate),
                DistanceToBeachMeters = listing.DistanceToBeachMeters,
                Latitude = listing.Latitude,
                Longitude = listing.Longitude,
                HasElevator = listing.HasElevator ?? false,
                HasTerrace = listing.HasTerrace,
                HasGarage = listing.HasGarage,
                HasSwimmingPool = listing.HasSwimmingPool,
                IsFurnished = listing.IsFurnished,
                HasParking = listing.HasParking,
                HasSeaView = listing.HasSeaView,
                HasCityView = listing.HasCityView,
            };
        }

        /// <summary>
        /// Reads one of the user's own properties. Everything here is nullable on the response,
        /// and a missing flag means "does not have it" - the same reading the comps get, so an
        /// owned property is never accidentally credited for a feature nobody recorded.
        /// </summary>
        public static ValuationSubject FromOwnedProperty(OwnedPropertyResponse property)
        {
            return new ValuationSubject
            {
                MarketAreaId = property.MarketAreaId,
                Typology = property.Typology,
                PropertyType = property.PropertyType,
                Condition = property.Condition ?? PropertyCondition.Good,
                AreaM2 = property.AreaM2,
                Bathrooms = property.Bathrooms,
                BalconyCount = property.BalconyCount ?? 0,
                Floor = property.Floor,
                ConstructionYear = property.ConstructionYear,
                EnergyGradeScore = ScoreEnergyGrade(property.EnergyCertificate),
                DistanceToBeachMeters = property.DistanceToBeachMeters,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                HasElevator = property.HasElevator ?? false,
                HasTerrace = property.HasTerrace ?? false,
                HasGarage = property.HasGarage ?? false,
                HasSwimmingPool = property.HasSwimmingPool ?? false,
                IsFurnished = property.IsFurnished ?? false,
                HasParking = property.HasParking ?? false,
                HasSeaView = property.HasSeaView ?? false,
                HasCityView = property.HasCityView ?? false,
            };
        }
    }
}
