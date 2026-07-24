
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
        /// Minimum listings needed on each side of a comparison (within one Typology)
        /// before that Typology's percentage counts toward the average. Below this,
        /// the comparison is too noisy to trust.
        /// </summary>
        private const int MinimumListingsPerGroup = 5;

        /// <summary>
        /// Calculates how much a feature (for example a garage) changes the price, as a
        /// percentage. Uses MATCHED comparisons: within each (PropertyType, Typology,
        /// MarketArea) group, and for every true/false combination of the OTHER tracked
        /// features, it compares listings that share that exact combination but differ
        /// only in the feature being tested. Each matched pair with enough listings on
        /// both sides contributes one ratio, and the final result is the geometric mean
        /// of those ratios (averaged in log space, see the note at the end of the method).
        ///
        /// Two things keep the comparison fair: we only look at a whitelist of dense,
        /// comparable towns (so a few far-off or thinly-covered areas can't add noise),
        /// and we group by PropertyType as well, so a T2 villa is never compared against
        /// a T2 apartment. Returns 0 if no group anywhere had enough data to trust.
        /// </summary>
        public static decimal CalculateFeaturePremiumPercent(List<PropertyListing> listings, string targetFeature)
        {
            // The only market areas we trust for comparison. Restricting to these dense,
            // well-covered towns makes every comparison group bigger and less noisy.
            // NormalizeText drops accents and case, so "Loule" and "Loulé" both match.
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

            // Keep only the listings we can actually use:
            //  - it has at least one snapshot (GetMedianPricePerM2 reads the newest one,
            //    so a listing with none would otherwise crash it), and
            //  - it sits in one of the whitelisted towns (matched on Town OR Municipality,
            //    because some sources fill only one of the two).
            listings = listings
                .Where(x => x.ListingSnapshots.Count > 0)
                .Where(x => x.MarketArea != null &&
                            (includedAreas.Contains(NormalizeText(x.MarketArea.Town)) ||
                             includedAreas.Contains(NormalizeText(x.MarketArea.Municipality))))
                .ToList();

            // Every tracked feature EXCEPT the one we're testing - these are the
            // features we hold constant (matched) on both sides of each comparison.
            var otherFeatures = TrackedFeatures.Where(f => f.Key != targetFeature).ToList();

            // With N "other" features, there are 2^N possible true/false patterns for
            // them (e.g. 4 other features -> 16 patterns, from "all off" to "all on").
            // "1 << otherFeatures.Count" is just a fast way to write 2^N.
            var combinationCount = 1 << otherFeatures.Count;

            // One LOG ratio per matched group that had enough data. We average these in
            // log space at the very end - see the note there for why not raw percentages.
            var logRatios = new List<double>();

            // Split listings into buckets by PropertyType AND Typology AND MarketArea (all
            // T2 apartments in Faro in one bucket, all T2 apartments in Loulé in another,
            // all T2 villas in Faro in yet another, ...) so we only ever compare a listing
            // to others of the same kind, same room layout, and same place.
            foreach (var bucket in listings.GroupBy(x => new { x.PropertyType, x.Typology, x.MarketAreaId }))
            {
                // Try every one of the 2^N patterns for the "other" features.
                // "combination" just counts 0, 1, 2, 3... up to combinationCount - 1.
                for (var combination = 0; combination < combinationCount; combination++)
                {
                    // Does this listing match the pattern we're currently trying?
                    // We read "combination" one BIT at a time: bit 0 says what
                    // otherFeatures[0] should be, bit 1 says what otherFeatures[1]
                    // should be, and so on. Example: with 4 other features, if
                    // combination = 5 (binary 0101), that means otherFeatures[0]=true,
                    // otherFeatures[1]=false, otherFeatures[2]=true, otherFeatures[3]=false.
                    // "1 << i" makes a number with only bit i turned on, and "combination
                    // & (1 << i)" keeps only that one bit from combination so we can
                    // check whether it was a 1 or a 0.
                    bool MatchesCombination(PropertyListing x)
                    {
                        for (var i = 0; i < otherFeatures.Count; i++)
                        {
                            var shouldBeTrue = (combination & (1 << i)) != 0;
                            var actuallyIs = otherFeatures[i].Value(x);

                            if (actuallyIs != shouldBeTrue)
                                return false; // doesn't match this pattern
                        }

                        return true; // every "other" feature matched this pattern
                    }

                    // Group A: HAS the feature we're testing, and matches this pattern
                    // for everything else.
                    var withFeature = bucket
                        .Where(x => TrackedFeatures[targetFeature](x) && MatchesCombination(x))
                        .ToList();

                    // Group B: does NOT have the feature we're testing, but matches the
                    // exact SAME pattern for everything else. Since A and B only differ
                    // in the one feature being tested, this is a fair, matched comparison.
                    var withoutFeature = bucket
                        .Where(x => !TrackedFeatures[targetFeature](x) && MatchesCombination(x))
                        .ToList();

                    if (withFeature.Count < MinimumListingsPerGroup || withoutFeature.Count < MinimumListingsPerGroup)
                        continue; // not enough listings on both sides for this pattern

                    var withFeatureMedian = GetMedianPricePerM2(withFeature);
                    var withoutFeatureMedian = GetMedianPricePerM2(withoutFeature);

                    // Both sides must be strictly positive to form a ratio and take its log.
                    if (withFeatureMedian <= 0 || withoutFeatureMedian <= 0)
                        continue;

                    // The ratio of the two medians: 1.20 means "has feature" costs 20% more.
                    // We store its natural log so we can average correctly below.
                    var ratio = (double)(withFeatureMedian / withoutFeatureMedian);

                    logRatios.Add(Math.Log(ratio));
                }
            }

            // Nothing anywhere had enough data on both sides -> nothing to report.
            if (logRatios.Count == 0)
                return 0;

            // Average in LOG space, then convert back, instead of averaging the raw
            // percentages. Percentages are asymmetric: +20% (x1.2) and the equal-and-
            // opposite -16.67% (/1.2) should cancel to 0%, but their plain average is
            // +1.67% - a made-up premium. Averaging the logs and undoing the log (this is
            // the geometric mean of the ratios) is symmetric, so opposite moves cancel.
            // (exp(meanLog) - 1) * 100 turns the average ratio back into a percentage.
            var meanLogRatio = logRatios.Average();

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

    }
}
