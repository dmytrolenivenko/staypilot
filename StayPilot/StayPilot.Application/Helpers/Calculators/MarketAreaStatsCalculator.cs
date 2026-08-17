using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Turns a pile of listings into one price row per place.
    ///
    /// The whole idea in one line: every listing is counted three times, once into its town,
    /// once into its municipality, once into its district. A flat in Guia raises the Guia row,
    /// the Albufeira row and the Faro row, all three.
    ///
    /// That is why the district numbers are not an average of the municipality numbers - they
    /// are worked out from the listings directly, so a municipality with 900 listings pulls the
    /// district figure harder than one with 12. Which is what you want.
    ///
    /// Zones are skipped on purpose: see <see cref="AreaLevel"/>.
    /// </summary>
    public static class MarketAreaStatsCalculator
    {
        /// <summary>
        /// Works out the stats for every place found in these listings.
        /// Listings with no price, no area or no market area are ignored - they cannot be placed.
        /// </summary>
        public static List<MarketAreaStats> Calculate(IEnumerable<PropertyListing> listings)
        {
            var calculatedAtUtc = DateTime.UtcNow;

            // One bucket per place: the place is the key, every €/m² we saw there is the value.
            var pricesByPlace = new Dictionary<PlaceKey, List<decimal>>();

            foreach (var listing in listings)
            {
                if (listing.MarketArea is null)
                {
                    continue;
                }

                var pricePerM2 = LatestPricePerM2(listing);

                if (pricePerM2 is null)
                {
                    continue;
                }

                var area = listing.MarketArea;

                // Here is the rolling up. The same price goes into three buckets.
                AddPrice(pricesByPlace, new PlaceKey(AreaLevel.District, area.District, string.Empty, string.Empty), pricePerM2.Value);
                AddPrice(pricesByPlace, new PlaceKey(AreaLevel.Municipality, area.District, area.Municipality, string.Empty), pricePerM2.Value);
                AddPrice(pricesByPlace, new PlaceKey(AreaLevel.Town, area.District, area.Municipality, area.Town), pricePerM2.Value);
            }

            var rows = new List<MarketAreaStats>();

            foreach (var (place, prices) in pricesByPlace)
            {
                prices.Sort();

                rows.Add(new MarketAreaStats
                {
                    Level = place.Level,
                    District = place.District,
                    Municipality = place.Municipality,
                    Town = place.Town,
                    ListingCount = prices.Count,
                    MedianPricePerM2 = decimal.Round(Median(prices), 2),
                    CalculatedAtUtc = calculatedAtUtc
                });
            }

            return rows;
        }

        /// <summary>
        /// The newest price for each square meter on this listing, or null when it has none we
        /// can use. A price of zero means the source did not give us one.
        /// </summary>
        private static decimal? LatestPricePerM2(PropertyListing listing)
        {
            var newest = listing.ListingSnapshots
                .OrderByDescending(x => x.SnapshotDateUtc)
                .FirstOrDefault();

            if (newest is null || newest.PricePerM2 <= 0)
            {
                return null;
            }

            return newest.PricePerM2;
        }

        /// <summary>
        /// Drops the price into its bucket, making the bucket if it is the first one.
        /// Places with a blank name are skipped: an empty name is missing data, not a place.
        /// </summary>
        private static void AddPrice(Dictionary<PlaceKey, List<decimal>> pricesByPlace, PlaceKey place, decimal pricePerM2)
        {
            if (place.HasBlankName)
            {
                return;
            }

            if (!pricesByPlace.TryGetValue(place, out var prices))
            {
                prices = new List<decimal>();
                pricesByPlace[place] = prices;
            }

            prices.Add(pricePerM2);
        }

        /// <summary>
        /// The middle value of an already sorted list.
        /// The middle and not the average, so one very expensive villa cannot drag a whole town up.
        /// </summary>
        private static decimal Median(List<decimal> sortedPrices)
        {
            var middle = sortedPrices.Count / 2;

            // An even count has no single middle value, so split the two in the middle.
            return sortedPrices.Count % 2 == 1
                ? sortedPrices[middle]
                : (sortedPrices[middle - 1] + sortedPrices[middle]) / 2m;
        }

        /// <summary>
        /// One place at one level. A record, so two listings in the same town land on the same
        /// key without us writing any comparison code.
        /// </summary>
        private readonly record struct PlaceKey(AreaLevel Level, string District, string Municipality, string Town)
        {
            /// <summary>
            /// True when the name this level needs is missing. A Town row needs all three names,
            /// a Municipality row needs two, a District row needs one.
            /// </summary>
            public bool HasBlankName => Level switch
            {
                AreaLevel.District => string.IsNullOrWhiteSpace(District),
                AreaLevel.Municipality => string.IsNullOrWhiteSpace(District) || string.IsNullOrWhiteSpace(Municipality),
                _ => string.IsNullOrWhiteSpace(District) || string.IsNullOrWhiteSpace(Municipality) || string.IsNullOrWhiteSpace(Town)
            };
        }
    }
}
