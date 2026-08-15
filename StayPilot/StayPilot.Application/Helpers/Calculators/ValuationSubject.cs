using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// A property in the exact terms the model prices on, so a scraped comp and one of the
    /// user's own flats are the same kind of thing to it.
    /// </summary>
    internal class ValuationSubject
    {
        /// <summary>Which market area the property sits in. Drives the location baseline.</summary>
        public int MarketAreaId { get; set; }

        /// <summary>
        /// The district and municipality the market area sits in, so a zone with too few
        /// listings to price on its own can still be priced as part of somewhere larger
        /// instead of falling back to the national average. Empty when unknown - the model
        /// then looks the geography up from the market area id.
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <inheritdoc cref="District"/>
        public string Municipality { get; set; } = string.Empty;

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
        /// Close enough to walk to the beach. One number, used everywhere the question comes up:
        /// the "Close to Beach" premium, and the sea view's beachfront figure. It lives here
        /// rather than on either calculator so the two can never disagree about what "close"
        /// means - which is exactly what happened when each kept its own copy.
        /// </summary>
        public const int CloseToBeachMeters = 500;

        /// <summary>
        /// Where being near the beach stops being worth anything at all. Between this and
        /// <see cref="CloseToBeachMeters"/> a property earns a shrinking share of the premium,
        /// so 501m is worth almost what 500m is instead of falling off a cliff.
        ///
        /// Two kilometres is a judgement, not a measurement - what was measured is "within 500m
        /// is worth X". It is the distance past which nobody calls a flat a beach property.
        /// </summary>
        public const int BeachCreditEndsAtMeters = 2000;

        /// <summary>
        /// The floor from which a lift stops being a convenience. Below it the premium does not
        /// clear zero at all; at and above it, it is one of the steadiest numbers we have.
        /// </summary>
        public const int LiftMattersFromFloor = 3;

        /// <summary>
        /// Nowhere in Portugal is 50km from the sea, so anything past this is a broken
        /// coordinate rather than an inland property.
        /// </summary>
        private const int ImplausibleBeachMeters = 50_000;

        /// <summary>
        /// Did this property come with a believable distance to the beach? Missing, zero and
        /// impossibly large all read the same way: we do not know. Callers must ask this before
        /// trusting <see cref="DistanceToBeachMeters"/>.
        /// </summary>
        public static bool KnowsBeachDistance(ValuationSubject subject)
        {
            return subject.DistanceToBeachMeters is > 0 and <= ImplausibleBeachMeters;
        }

        /// <summary>
        /// Is this property within <see cref="CloseToBeachMeters"/> of the sea? False when we do
        /// not know the distance - "we never measured it" is not evidence of being close.
        /// </summary>
        public static bool IsCloseToBeach(ValuationSubject subject)
        {
            return KnowsBeachDistance(subject) && subject.DistanceToBeachMeters <= CloseToBeachMeters;
        }

        /// <summary>
        /// High enough up that a lift is worth real money. False when the floor was never
        /// stated, for the same reason as above: a gap is not a ground floor.
        /// </summary>
        public static bool IsHighUp(ValuationSubject subject)
        {
            return subject.Floor >= LiftMattersFromFloor;
        }

        /// <summary>
        /// Certificate letter to a position on the scale, so the model prices one step rather
        /// than nine letters. Unrecognised comes back null, not "G" - a typo is missing data,
        /// not the worst possible rating.
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
        /// The letter behind a score - the inverse of <see cref="ScoreEnergyGrade"/>, kept
        /// beside it so the two can't drift. Off-scale values clamp to the ends.
        /// </summary>
        public static string GradeLetter(int score)
        {
            return Math.Clamp(score, 0, 8) switch
            {
                8 => "A+",
                7 => "A",
                6 => "B",
                5 => "B-",
                4 => "C",
                3 => "D",
                2 => "E",
                1 => "F",
                _ => "G",
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

                // Null whenever the caller did not Include the market area. Empty strings just
                // mean "unknown", which the location fallback already handles.
                District = listing.MarketArea?.District ?? string.Empty,
                Municipality = listing.MarketArea?.Municipality ?? string.Empty,
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

    /// <summary>
    /// Which listings are fit to learn from. The scraper occasionally reads the wrong number as
    /// the floor area - a T3 that comes back as 2 m² with its €349,000 price intact - and the
    /// resulting €174,500/m² is not an expensive flat, it is a broken row.
    ///
    /// These are not cosmetic. Both fits minimise SQUARED error on ln(price/m²), so one listing
    /// off by a factor of 40 carries the weight of roughly two hundred ordinary ones and drags
    /// every coefficient with it. The old rule admitted anything between €100 and €100,000/m².
    ///
    /// One place, because the price model, the premium measurements and the backtest must agree
    /// on what counts as real data: score a valuation against a row the model refuses to learn
    /// from and you are measuring the scraper, not the valuation.
    /// </summary>
    internal static class ListingQuality
    {
        /// <summary>
        /// Circulation, kitchen and bathroom, before any bedroom is added. A home cannot be
        /// smaller than this no matter how it is described.
        /// </summary>
        private const int BaseAreaM2 = 20;

        /// <summary>Floor space per bedroom. Deliberately below any real room.</summary>
        private const int AreaPerBedroomM2 = 10;

        /// <summary>
        /// Nothing residential in Portugal is priced below this per m². The genuinely cheap
        /// interior runs to about €600, so this only catches parking spaces sold as flats,
        /// "price on application" placeholders, and prices that lost three digits.
        /// </summary>
        private const decimal MinimumPricePerM2 = 400m;

        /// <summary>
        /// Prime Lisbon and the Quinta do Lago strip reach roughly €12,000/m². Past double that
        /// is a broken area, not a trophy home - and the ones we checked all had areas in the
        /// single digits.
        /// </summary>
        private const decimal MaximumPricePerM2 = 25_000m;

        /// <summary>An apartment larger than this is a mis-parse or a whole building.</summary>
        private const int MaximumAreaM2 = 1_000;

        /// <summary>
        /// The smallest floor area a property of this typology could really have. The enum
        /// counts from T0 = 1, so bedrooms is one less than the stored value.
        /// </summary>
        public static int MinimumPlausibleAreaM2(Typology typology)
        {
            var bedrooms = Math.Max(0, (int)typology - 1);

            return BaseAreaM2 + (bedrooms * AreaPerBedroomM2);
        }

        /// <summary>
        /// Is this listing, at this snapshot, real enough to learn from? Everything rejected here
        /// is a data fault rather than an unusual property - the bounds sit well outside the real
        /// market on both sides, below the 1st percentile of every typology.
        /// </summary>
        public static bool IsUsable(PropertyListing listing, ListingSnapshot? snapshot)
        {
            if (snapshot is null)
                return false;

            if (listing.AreaM2 < MinimumPlausibleAreaM2(listing.Typology) || listing.AreaM2 > MaximumAreaM2)
                return false;

            return snapshot.PricePerM2 >= MinimumPricePerM2 && snapshot.PricePerM2 <= MaximumPricePerM2;
        }

        /// <summary>The newest snapshot, which is the one every price decision is made on.</summary>
        public static ListingSnapshot? NewestSnapshot(PropertyListing listing)
        {
            return listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();
        }

        /// <summary>
        /// Every listing worth learning from, already paired with its price on the log scale -
        /// the exact input both fits start from. Having one method do this is what keeps the
        /// price model and the premium measurements looking at the same market.
        /// </summary>
        public static List<(ValuationSubject Subject, double LogPricePerM2)> UsableSubjects(
            IEnumerable<PropertyListing> listings)
        {
            var usable = new List<(ValuationSubject, double)>();

            foreach (var listing in listings)
            {
                var snapshot = NewestSnapshot(listing);

                if (!IsUsable(listing, snapshot))
                    continue;

                usable.Add((ValuationSubject.FromListing(listing), Math.Log((double)snapshot!.PricePerM2)));
            }

            return usable;
        }
    }
}
