using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Domain.Entities;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    /// <summary>
    /// Turns a pile of listings into one row of numbers per place.
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
    ///
    /// It runs in three plain steps, one method each:
    ///   1. <see cref="CollectByPlace"/> - drop every listing into its three buckets.
    ///   2. <see cref="BuildRow"/>       - turn one bucket into one saved row.
    ///   3. <see cref="BuildTypologyRows"/> - and its per-typology children.
    /// </summary>
    public static class MarketAreaStatsCalculator
    {
        /// <summary>
        /// The fewest listings needed before a median is worth saving for one of the smaller
        /// splits (a single typology, the project stock). The parent row has no such limit -
        /// it saves whatever it found and the reader decides, see the repository.
        /// </summary>
        private const int MinimumForSplit = 3;

        /// <summary>
        /// Works out the stats for every place found in these listings.
        /// Listings with no price or no area are ignored - they cannot be placed.
        /// </summary>
        public static List<MarketAreaStats> Calculate(IEnumerable<MarketAreaStatsListingRow> listings)
        {
            var allListings = listings.ToList();

            var placesFound = CollectByPlace(allListings);

            var rows = new List<MarketAreaStats>();
            var calculatedAtUtc = DateTime.UtcNow;

            foreach (var place in placesFound.Keys)
            {
                rows.Add(BuildRow(place, placesFound[place], calculatedAtUtc));
            }

            return rows;
        }

        /// <summary>
        /// Step 1. Walks the listings once and drops each one into its district bucket, its
        /// municipality bucket and its town bucket.
        /// </summary>
        private static Dictionary<PlaceKey, PlaceListings> CollectByPlace(List<MarketAreaStatsListingRow> listings)
        {
            var placesFound = new Dictionary<PlaceKey, PlaceListings>();

            foreach (var listing in listings)
            {
                if (listing.PricePerM2 <= 0 || listing.AreaM2 <= 0)
                {
                    continue;
                }

                // Here is the rolling up. The same listing goes into three buckets.
                AddToPlace(placesFound, new PlaceKey(AreaLevel.District, listing.District, string.Empty, string.Empty), listing);
                AddToPlace(placesFound, new PlaceKey(AreaLevel.Municipality, listing.District, listing.Municipality, string.Empty), listing);
                AddToPlace(placesFound, new PlaceKey(AreaLevel.Town, listing.District, listing.Municipality, listing.Town), listing);
            }

            return placesFound;
        }

        /// <summary>
        /// Step 2. Turns one place's collected listings into the row we save.
        /// </summary>
        private static MarketAreaStats BuildRow(PlaceKey place, PlaceListings collected, DateTime calculatedAtUtc)
        {
            var row = new MarketAreaStats
            {
                Level = place.Level,
                District = place.District,
                Municipality = place.Municipality,
                Town = place.Town,
                ListingCount = collected.PricesPerM2.Count,
                MedianPricePerM2 = Median(collected.PricesPerM2),
                MedianAreaM2 = Median(collected.Areas),

                // No pricing model backs a "below estimate" flag any more - a comp-median-based
                // deals check can replace this later if it's wanted back.
                BelowEstimateCount = 0,
                CentroidLatitude = collected.AverageLatitude(),
                CentroidLongitude = collected.AverageLongitude(),
                ProjectCount = collected.ProjectPricesPerM2.Count,
                ProjectByConditionCount = collected.ProjectByConditionCount,
                ProjectByEnergyCount = collected.ProjectByEnergyCount,
                ProjectMedianPricePerM2 = MedianOrNull(collected.ProjectPricesPerM2),
                ProjectMedianAreaM2 = MedianOrNull(collected.ProjectAreas),
                ProjectP25PricePerM2 = PercentileOrNull(collected.ProjectPricesPerM2, 0.25),
                ProjectP75PricePerM2 = PercentileOrNull(collected.ProjectPricesPerM2, 0.75),
                MoveInCount = collected.MoveInPricesPerM2.Count,
                MoveInMedianPricePerM2 = MedianOrNull(collected.MoveInPricesPerM2),
                MoveInMedianAreaM2 = MedianOrNull(collected.MoveInAreas),
                MoveInP25PricePerM2 = PercentileOrNull(collected.MoveInPricesPerM2, 0.25),
                MoveInP75PricePerM2 = PercentileOrNull(collected.MoveInPricesPerM2, 0.75),
                UnclassifiedCount = collected.UnclassifiedCount,
                CalculatedAtUtc = calculatedAtUtc
            };

            row.TypologyStats = BuildTypologyRows(collected);

            return row;
        }

        /// <summary>
        /// Step 3. One child row per typology this place has enough listings of.
        /// </summary>
        private static List<MarketAreaTypologyStats> BuildTypologyRows(PlaceListings collected)
        {
            var typologyRows = new List<MarketAreaTypologyStats>();

            foreach (var typology in collected.ByTypology.Keys)
            {
                var forTypology = collected.ByTypology[typology];

                // A "median T2 price" taken from one advert is that advert, and a budget screen
                // reading it would send you shopping on a single listing.
                if (forTypology.Prices.Count < MinimumForSplit)
                {
                    continue;
                }

                typologyRows.Add(new MarketAreaTypologyStats
                {
                    Typology = typology,
                    ListingCount = forTypology.Prices.Count,
                    MedianPrice = Median(forTypology.Prices),
                    MedianAreaM2 = Median(forTypology.Areas),
                    MedianPricePerM2 = Median(forTypology.PricesPerM2)
                });
            }

            return typologyRows;
        }

        /// <summary>
        /// True when the listing looks like a renovation project.
        ///
        /// The advert's own "needs renovation" is not enough on its own: it is set on about 1.4%
        /// of listings, so most places would have two or three and no measurable discount. A poor
        /// energy certificate covers roughly ten times as many and is the more objective signal -
        /// a grade is measured, "needs work" is whatever the agent felt like typing.
        /// </summary>
        private static bool IsProject(MarketAreaStatsListingRow listing)
        {
            if (listing.Condition == PropertyCondition.NeedsRenovation)
            {
                return true;
            }

            return EnergyGradeLetter(listing.EnergyCertificate) is 'D' or 'E' or 'F' or 'G';
        }

        /// <summary>
        /// True when the listing is ready to move into, which is what a project is discounted
        /// against. Anything neither project nor move-in (an unknown condition with no
        /// certificate) counts for neither, so the comparison stays between two clear groups.
        /// </summary>
        private static bool IsMoveInReady(MarketAreaStatsListingRow listing)
        {
            if (IsProject(listing))
            {
                return false;
            }

            return listing.Condition is PropertyCondition.Good
                or PropertyCondition.Renovated
                or PropertyCondition.NewBuild;
        }

        /// <summary>
        /// The grade letter off a certificate, so "A+", "A" and "B-" read as A, A and B.
        /// Null when the certificate is missing or is not a grade we recognise.
        /// </summary>
        private static char? EnergyGradeLetter(string? energyCertificate)
        {
            if (string.IsNullOrWhiteSpace(energyCertificate))
            {
                return null;
            }

            var letter = char.ToUpperInvariant(energyCertificate.Trim()[0]);

            return letter is >= 'A' and <= 'G' ? letter : null;
        }

        /// <summary>
        /// Adds one listing to one place's bucket, making the bucket if it is the first one.
        /// Places with a blank name are skipped: an empty name is missing data, not a place.
        /// </summary>
        private static void AddToPlace(
            Dictionary<PlaceKey, PlaceListings> placesFound,
            PlaceKey place,
            MarketAreaStatsListingRow listing)
        {
            if (place.HasBlankName)
            {
                return;
            }

            if (!placesFound.TryGetValue(place, out var collected))
            {
                collected = new PlaceListings();
                placesFound[place] = collected;
            }

            collected.Add(listing);
        }

        /// <summary>
        /// The middle value of a list. Sorts the list in place first.
        /// The middle and not the average, so one very expensive villa cannot drag a town up.
        /// </summary>
        private static decimal Median(List<decimal> values)
        {
            values.Sort();

            var middle = values.Count / 2;

            // An even count has no single middle value, so split the two in the middle.
            return values.Count % 2 == 1
                ? decimal.Round(values[middle], 2)
                : decimal.Round((values[middle - 1] + values[middle]) / 2m, 2);
        }

        /// <summary>
        /// <see cref="Median"/>, but null when there is too little to take a median from.
        /// </summary>
        private static decimal? MedianOrNull(List<decimal> values)
        {
            return values.Count < MinimumForSplit ? null : Median(values);
        }

        /// <summary>
        /// One point of the spread, or null when there is too little to have a spread. Gated at
        /// the same count as the median it sits beside, so a row never carries a quartile it has
        /// no median for.
        /// </summary>
        private static decimal? PercentileOrNull(List<decimal> values, double percentile)
        {
            if (values.Count < MinimumForSplit)
            {
                return null;
            }

            values.Sort();

            return decimal.Round(Calculator.Percentile(values, percentile), 2);
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

        /// <summary>
        /// Everything collected for one place while walking the listings. Plain lists on purpose:
        /// the medians need the values kept, not running totals, and a list you can look at in
        /// the debugger beats a clever accumulator.
        /// </summary>
        private class PlaceListings
        {
            public List<decimal> PricesPerM2 { get; } = new();

            public List<decimal> Areas { get; } = new();

            /// <summary>Price for each square meter of the stock that needs work.</summary>
            public List<decimal> ProjectPricesPerM2 { get; } = new();

            /// <summary>Floor area of the same, so the discount can be turned into money.</summary>
            public List<decimal> ProjectAreas { get; } = new();

            /// <summary>Price for each square meter of the stock that does not.</summary>
            public List<decimal> MoveInPricesPerM2 { get; } = new();

            /// <inheritdoc cref="ProjectAreas"/>
            public List<decimal> MoveInAreas { get; } = new();

            /// <summary>Projects the advert itself called out as needing work.</summary>
            public int ProjectByConditionCount { get; private set; }

            /// <summary>Projects caught only by a poor energy grade.</summary>
            public int ProjectByEnergyCount { get; private set; }

            /// <summary>Listings that are neither, so they sit out of the comparison.</summary>
            public int UnclassifiedCount { get; private set; }

            public Dictionary<Typology, TypologyListings> ByTypology { get; } = new();

            private readonly List<decimal> _latitudes = new();
            private readonly List<decimal> _longitudes = new();

            public void Add(MarketAreaStatsListingRow listing)
            {
                PricesPerM2.Add(listing.PricePerM2);
                Areas.Add(listing.AreaM2);

                if (IsProject(listing))
                {
                    ProjectPricesPerM2.Add(listing.PricePerM2);
                    ProjectAreas.Add(listing.AreaM2);

                    // Which of the two signals caught it. The advert's own word wins when both
                    // apply, so the two counts add up to the project count rather than overlapping.
                    if (listing.Condition == PropertyCondition.NeedsRenovation)
                    {
                        ProjectByConditionCount++;
                    }
                    else
                    {
                        ProjectByEnergyCount++;
                    }
                }
                else if (IsMoveInReady(listing))
                {
                    MoveInPricesPerM2.Add(listing.PricePerM2);
                    MoveInAreas.Add(listing.AreaM2);
                }
                else
                {
                    // Neither. Counted so the screen can say how much of the stock the discount
                    // has no opinion about, instead of quietly resting on a third of the market.
                    UnclassifiedCount++;
                }

                _latitudes.Add(listing.Latitude);
                _longitudes.Add(listing.Longitude);

                if (!ByTypology.TryGetValue(listing.Typology, out var forTypology))
                {
                    forTypology = new TypologyListings();
                    ByTypology[listing.Typology] = forTypology;
                }

                forTypology.Prices.Add(listing.Price);
                forTypology.PricesPerM2.Add(listing.PricePerM2);
                forTypology.Areas.Add(listing.AreaM2);
            }

            /// <summary>
            /// Middle point of the listings, or null when there are none.
            /// </summary>
            public decimal? AverageLatitude()
            {
                return _latitudes.Count == 0 ? null : decimal.Round(_latitudes.Average(), 6);
            }

            /// <inheritdoc cref="AverageLatitude"/>
            public decimal? AverageLongitude()
            {
                return _longitudes.Count == 0 ? null : decimal.Round(_longitudes.Average(), 6);
            }
        }

        /// <summary>
        /// The same, for one typology inside one place.
        /// </summary>
        private class TypologyListings
        {
            public List<decimal> Prices { get; } = new();

            public List<decimal> PricesPerM2 { get; } = new();

            public List<decimal> Areas { get; } = new();
        }
    }
}
