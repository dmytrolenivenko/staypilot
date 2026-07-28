
using StayPilot.Application.Contracts.Request;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;
using System.Globalization;
using System.Text;

namespace StayPilot.Application.Helpers.Calculators
{
    public class Calculator
    {
        /// <summary>
        /// Clean text so two names can be compared safely.
        /// It makes the text lower case, removes accents (á becomes a), and trims spaces.
        /// Example: "  Faró " becomes "faro".
        /// </summary>
        public static string NormalizeText(string value)
        {
            // Empty or spaces only -> return empty text.
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Lower case, no spaces at the ends, and split each accent from its letter.
            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            // Keep every character, but drop the accent marks.
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            // Put the text back together in the normal form.
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Find which market area a property belongs to, using its address.
        /// We try an exact match first. If that fails, we try easier matches.
        /// </summary>
        public static int GetMarketId(List<MarketArea> marketAreas, string countryRaw, string districtRaw, string municipalityRaw, string townRaw, string zoneRaw = "")
        {
            // Clean each address part so we can compare it safely (see NormalizeText).
            var country = NormalizeText(countryRaw);
            var district = NormalizeText(districtRaw);
            var municipality = NormalizeText(municipalityRaw);
            var town = NormalizeText(townRaw);
            var zone = NormalizeText(zoneRaw ?? string.Empty);

            // Try 1: exact match on all parts (country, district, municipality, town, zone).
            var marketArea = marketAreas.FirstOrDefault(x =>
                NormalizeText(x.Country) == country &&
                NormalizeText(x.District) == district &&
                NormalizeText(x.Municipality) == municipality &&
                NormalizeText(x.Town) == town &&
                NormalizeText(x.Zone ?? string.Empty) == zone);

            // Try 2: many listings have no Zone (the source does not give it).
            // So match by Town only. Prefer a market area that also has no Zone.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality &&
                    NormalizeText(x.Town) == town)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            // Try 3: some listings only give the Municipality (no Town).
            // So match by Municipality only. Someone can fix the exact zone later by hand.
            marketArea ??= marketAreas
                .Where(x =>
                    NormalizeText(x.Country) == country &&
                    NormalizeText(x.District) == district &&
                    NormalizeText(x.Municipality) == municipality)
                .OrderBy(x => x.Zone == null ? 0 : 1)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            // Still nothing -> we cannot place this property. Stop with an error.
            if (marketArea == null)
                throw new InvalidOperationException("Market area not found.");

            return marketArea.Id;
        }

        /// <summary>
        /// Find the beach nearest to the property.
        /// Returns null if the property has no location.
        /// </summary>
        public static BeachMarker? GetTheClosestBeach(List<BeachMarker> beaches, decimal? lat, decimal? lon)
        {
            // Fix: Lat/Lon were "double" before, so this null check could never work
            // (a double can never be null). We use "decimal?" here so we can check
            // for a missing location, and only turn it into "double" later, for the math.
            if (lat is null || lon is null)
            {
                return null;
            }

            var propertyLat = (double)lat.Value;
            var propertyLon = (double)lon.Value;

            // Sort all beaches by distance to the property and take the closest one.
            var closestBeach = beaches
                .OrderBy(beach => CalculateDistanceMeters(
                    propertyLat,
                    propertyLon,
                    (double)beach.Latitude,
                    (double)beach.Longitude))
                .FirstOrDefault();

            return closestBeach;
        }

        /// <summary>
        /// Distance in meters between two points on Earth (given as latitude/longitude).
        /// </summary>
        public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula: the standard way to measure distance on a globe.
            const double earthRadiusMeters = 6371000;

            double ToRadians(double degrees)
            {
                return degrees * Math.PI / 180;
            }

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) *
                Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMeters * c;
        }

        /// <summary>
        /// Boolean features tracked for the price-premium calculation, and how to read
        /// each one off a PropertyListing. HasAirConditioning/HasCityView are excluded:
        /// AC is never recorded false in this dataset, and City View is never populated
        /// at all (both are permanent data gaps, not fixable by comparing differently).
        /// Parking and SeaView are included despite low counts (16 and ~7 pre-backfill) -
        /// worst case they just come back as 0 for lack of matching data, same as any
        /// other feature that can't clear the minimum listings threshold.
        /// </summary>
        // This is a lookup table from a feature NAME (string) to a small function that
        // checks that one feature on a listing. Each function is written as "x => ..."
        // (a lambda): "x" stands for whatever PropertyListing gets passed in later, and
        // the part after "=>" is what gets returned (true/false).
        //
        // Storing functions like this means CalculateFeaturePremiumPercent below doesn't
        // need to know which feature it's checking - it just does
        // TrackedFeatures["HasGarage"](someListing) to look up the right function AND
        // run it in one step, instead of a separate hardcoded if-check per feature.
        private static readonly Dictionary<string, Func<PropertyListing, bool>> TrackedFeatures = new()
        {
            ["HasElevator"] = x => x.HasElevator ?? false, // ?? false: treat "unknown" (null) as "doesn't have it"
            ["HasTerrace"] = x => x.HasTerrace,
            ["HasGarage"] = x => x.HasGarage,
            ["HasSwimmingPool"] = x => x.HasSwimmingPool,
            ["IsFurnished"] = x => x.IsFurnished,
            ["HasParking"] = x => x.HasParking,
            ["HasSeaView"] = x => x.HasSeaView,
            ["IsNewBuild"] = x => x.Condition == PropertyCondition.NewBuild,
            ["IsRenovated"] = x => x.Condition == PropertyCondition.Renovated,
        };

        /// <summary>
        /// Names of every feature tracked for the price-premium calculation - the single
        /// source of truth for "which features do we calculate." Callers (like the
        /// service that recalculates all of them) should loop over this instead of
        /// keeping their own separate hardcoded list, so the two can never drift apart.
        /// </summary>
        public static IReadOnlyCollection<string> TrackedFeatureNames => TrackedFeatures.Keys;

        /// <summary>
        /// Minimum listings needed on each side of a comparison (within one bucket)
        /// before that bucket's percentage counts toward the average. Below this,
        /// the comparison is too noisy to trust.
        /// </summary>
        private const int MinimumListingsPerGroup = 5;

        /// <summary>
        /// The features held constant (matched) inside every bucket by default: the two
        /// biggest structural value drivers - a garage and a sea view. Matching on these
        /// two removes most of the "shared credit" bias (features that travel together
        /// inflating each other) while keeping buckets large. Matching on MORE than a
        /// couple starves the thin features (e.g. sea view); matching on ALL of them is
        /// what broke the old version. Chosen on plain real-estate logic (a car space and
        /// a sea view are the obvious big extras), not tuned to hit any target.
        /// </summary>
        private static readonly IReadOnlyCollection<string> DefaultPremiumControls =
            new[] { "HasGarage", "HasSeaView" };

        /// <summary>
        /// Calculates how much a feature (for example a garage) changes the price, as a
        /// percentage. Compares like-with-like: within each bucket of listings that share
        /// the same PropertyType, Typology and MarketArea - AND the same on/off pattern of
        /// the <see cref="DefaultPremiumControls"/> (garage, sea view) - it takes the median
        /// price/m² of listings that HAVE the feature versus those that don't as a ratio,
        /// and combines those ratios across buckets as a weighted geometric mean. Returns 0
        /// if no bucket has enough data on both sides to trust.
        ///
        /// This is the default entry point; it forwards to the overload below with the
        /// standard control set. See that overload for the full explanation of the math and
        /// of why we match on only a couple of features instead of all of them.
        /// </summary>
        public static decimal CalculateFeaturePremiumPercent(List<PropertyListing> listings, string targetFeature)
            => CalculateFeaturePremiumPercent(listings, targetFeature, DefaultPremiumControls);

        /// <summary>
        /// Same idea as the two-argument <see cref="CalculateFeaturePremiumPercent(List{PropertyListing}, string)"/>,
        /// but it also holds a SHORT list of extra features constant inside each bucket -
        /// the handful of big, structural value drivers (a garage, a sea view, a new build)
        /// that most often "travel with" other features and inflate their premium. Matching
        /// on just those few removes most of the shared-credit bias while keeping buckets
        /// large - the honest middle ground between the plain bucket method and the full
        /// regression. It is still only medians and ratios; nothing is fitted to a target
        /// answer. Matching on ALL nine features at once is what starved the old version.
        /// </summary>
        public static decimal CalculateFeaturePremiumPercent(
            List<PropertyListing> listings,
            string targetFeature,
            IReadOnlyCollection<string> controlFeatures)
        {
            listings = FilterToComparableListings(listings);

            // Never match on the feature we're pricing; keep only real, known controls.
            var controls = controlFeatures
                .Where(c => c != targetFeature && TrackedFeatures.ContainsKey(c))
                .ToList();

            var logRatios = new List<(double LogRatio, double Weight)>();

            // Bucket by kind/size/location as before, PLUS the on/off pattern of the control
            // features - so within a bucket every listing also shares those. The key is just
            // "PropertyType|Typology|MarketArea|<control bits>", e.g. "2|3|17|101".
            foreach (var bucket in listings.GroupBy(x =>
                         $"{(int)x.PropertyType}|{(int)x.Typology}|{x.MarketAreaId}|" +
                         string.Concat(controls.Select(c => TrackedFeatures[c](x) ? '1' : '0'))))
            {
                var withFeature = bucket.Where(x => TrackedFeatures[targetFeature](x)).ToList();
                var withoutFeature = bucket.Where(x => !TrackedFeatures[targetFeature](x)).ToList();

                if (withFeature.Count < MinimumListingsPerGroup || withoutFeature.Count < MinimumListingsPerGroup)
                    continue;

                var withFeatureMedian = GetMedianPricePerM2(withFeature);
                var withoutFeatureMedian = GetMedianPricePerM2(withoutFeature);

                if (withFeatureMedian <= 0 || withoutFeatureMedian <= 0)
                    continue;

                var ratio = (double)(withFeatureMedian / withoutFeatureMedian);
                var weight = Math.Min(withFeature.Count, withoutFeature.Count);

                logRatios.Add((Math.Log(ratio), weight));
            }

            // No bucket had enough data on both sides -> nothing to report.
            if (logRatios.Count == 0)
                return 0;

            // Combine in LOG space (a weighted geometric mean of the ratios): averaging the
            // logs makes +20% and -16.67% cancel to 0% instead of a made-up +1.67%, and the
            // weight lets better-supported buckets count more. (e^mean - 1) * 100 is the %.
            var totalWeight = logRatios.Sum(x => x.Weight);
            var meanLogRatio = logRatios.Sum(x => x.LogRatio * x.Weight) / totalWeight;

            return (decimal)((Math.Exp(meanLogRatio) - 1) * 100);
        }

        /// <summary>
        /// Median PricePerM2 across a list of listings, using each one's newest snapshot.
        /// </summary>
        private static decimal GetMedianPricePerM2(List<PropertyListing> listings)
        {
            var sortedPrices = listings
                .Select(x => x.ListingSnapshots.First().PricePerM2)
                .OrderBy(x => x)
                .ToList();

            var count = sortedPrices.Count;

            return count % 2 != 0
                ? sortedPrices[count / 2]
                : (sortedPrices[count / 2] + sortedPrices[(count / 2) - 1]) / 2;
        }

        /// <summary>
        /// Keeps only the listings we can fairly compare: at least one price snapshot, and
        /// located in one of the dense, well-covered whitelist towns (matched on Town OR
        /// Municipality, because some sources fill only one). NormalizeText drops accents
        /// and case, so "Loule" and "Loulé" both match. Shared by both premium calculators
        /// so they always judge the exact same set of listings.
        /// </summary>
        private static List<PropertyListing> FilterToComparableListings(List<PropertyListing> listings)
        {
            var includedAreas = new HashSet<string>
            {
                NormalizeText("Albufeira"),
                NormalizeText("Faro"),
                NormalizeText("Quarteira"),
                NormalizeText("Loulé"),
                NormalizeText("Lagos"),
                NormalizeText("Lagoa"),
                NormalizeText("Tavira"),
            };

            return listings
                .Where(x => x.ListingSnapshots.Count > 0)
                .Where(x => x.MarketArea != null &&
                            (includedAreas.Contains(NormalizeText(x.MarketArea.Town)) ||
                             includedAreas.Contains(NormalizeText(x.MarketArea.Municipality))))
                .ToList();
        }

    }
}
