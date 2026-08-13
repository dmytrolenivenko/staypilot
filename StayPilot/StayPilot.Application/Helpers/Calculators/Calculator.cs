
using StayPilot.Domain.Entities;
using System.Globalization;
using System.Text;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// General-purpose maths: text matching, market areas, beaches, distance, statistics.
    /// Valuations and premium features do NOT belong here - see
    /// <see cref="PremiumFeaturesCalculator"/> and <see cref="OwnedPropertyValuationCalculator"/>.
    /// </summary>
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
        /// Middle value of an ascending-sorted list. 0 when empty.
        /// </summary>
        public static decimal Median(IReadOnlyList<decimal> sortedAscending)
        {
            return Percentile(sortedAscending, 0.5);
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
