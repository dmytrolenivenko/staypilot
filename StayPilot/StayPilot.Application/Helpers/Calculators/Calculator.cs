using StayPilot.Domain.Entities;
using System.Globalization;
using System.Text;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Shared calculation helpers used across the valuation pipeline: address matching,
    /// geo distance, and basic statistics (median, percentile, weighted average).
    /// </summary>
    public class Calculator
    {
        // ----- Address matching -----------------------------------------------------------

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
        /// Returns null when the address matches nothing, so the caller can report it as an
        /// error on its own response instead of catching an exception.
        /// </summary>
        public static int? GetMarketId(List<MarketArea> marketAreas, string countryRaw, string districtRaw, string municipalityRaw, string townRaw, string zoneRaw = "")
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

            // Still nothing -> we cannot place this property. The caller turns that into an error.
            return marketArea?.Id;
        }

        /// <summary>
        /// The address parts as one readable line, for the error we show when none of them
        /// match a market area. Blank parts are left out.
        /// </summary>
        public static string DescribeAddress(string? country, string? district, string? municipality, string? town, string? zone)
        {
            var parts = new[] { country, district, municipality, town, zone }
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return string.Join(", ", parts);
        }

        // ----- Geo distance -----------------------------------------------------------------

        /// <summary>How many metres one degree of latitude covers, anywhere on Earth.</summary>
        public const double MetersPerDegreeLatitude = 111_320;

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
        /// How much a degree of longitude shrinks at this latitude, relative to a degree of
        /// latitude - multiply a longitude delta in degrees by this before comparing it against a
        /// latitude delta, so a circle measured in metres doesn't come out an ellipse in degrees.
        /// </summary>
        public static decimal LongitudeDegreeScale(decimal atLatitude) =>
            (decimal)Math.Cos((double)atLatitude * Math.PI / 180);

        /// <summary>
        /// A radius in metres, converted to degrees of latitude and squared - ready to compare
        /// against <c>(Δlat)² + (Δlon × <see cref="LongitudeDegreeScale"/>)²</c> without ever
        /// taking a square root.
        /// </summary>
        public static decimal RadiusDegreesSquared(double radiusMeters)
        {
            var radiusDegrees = (decimal)(radiusMeters / MetersPerDegreeLatitude);

            return radiusDegrees * radiusDegrees;
        }

        // ----- Statistics --------------------------------------------------------------------

        /// <summary>
        /// Middle value of an ascending-sorted list. 0 when empty.
        /// </summary>
        public static decimal Median(IReadOnlyList<decimal> sortedAscending)
        {
            return Percentile(sortedAscending, 0.5);
        }

        /// <inheritdoc cref="Median(IReadOnlyList{decimal})"/>
        public static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToList();

            if (sorted.Count == 0)
                return 0;

            return sorted.Count % 2 != 0
                ? sorted[sorted.Count / 2]
                : (sorted[sorted.Count / 2] + sorted[(sorted.Count / 2) - 1]) / 2;
        }

        /// <summary>
        /// The middle value when some observations count for more than others: half the weight
        /// below it, half above. Used to summarise comparables, where one 200m away says far more
        /// about a property than one 2km away, and a plain median pretends they say the same.
        /// </summary>
        public static decimal WeightedMedian(IReadOnlyList<(decimal Value, double Weight)> weighted)
        {
            if (weighted.Count == 0)
                return 0;

            var ordered = weighted.OrderBy(x => x.Value).ToList();
            var half = ordered.Sum(x => x.Weight) / 2;
            var running = 0d;

            foreach (var (value, weight) in ordered)
            {
                running += weight;

                if (running >= half)
                    return value;
            }

            return ordered[^1].Value;
        }

        /// <inheritdoc cref="WeightedMedian"/>
        public static decimal WeightedAverage(IReadOnlyList<(decimal Value, double Weight)> weighted)
        {
            var totalWeight = weighted.Sum(x => x.Weight);

            if (weighted.Count == 0 || totalWeight <= 0)
                return 0;

            return weighted.Sum(x => x.Value * (decimal)x.Weight) / (decimal)totalWeight;
        }

        /// <summary>
        /// Value at a percentile (0.0-1.0) of an ascending-sorted list, interpolated.
        /// Used instead of raw min/max so one freak listing can't define a range.
        /// </summary>
        public static decimal Percentile(IReadOnlyList<decimal> sortedAscending, double percentile)
        {
            if (sortedAscending.Count == 0)
                return 0;

            if (sortedAscending.Count == 1)
                return sortedAscending[0];

            var rank = percentile * (sortedAscending.Count - 1);
            var lowIndex = (int)Math.Floor(rank);
            var highIndex = (int)Math.Ceiling(rank);

            if (lowIndex == highIndex)
                return sortedAscending[lowIndex];

            var weight = (decimal)(rank - lowIndex);

            return sortedAscending[lowIndex] * (1 - weight) + sortedAscending[highIndex] * weight;
        }
    }
}
