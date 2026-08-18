
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// Price numbers for one place, worked out from all the listings we have there.
    ///
    /// One row per place per level, so the same listing counts three times: once into its
    /// town row, once into its municipality row, once into its district row.
    ///
    /// Nothing here is typed in by hand. The whole table is rebuilt each time the stats are
    /// recalculated, the same way <see cref="PremiumFeature"/> is.
    /// </summary>
    public class MarketAreaStats
    {
        /// <summary>
        /// Database Id for this row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Which level this row measures: a district, a municipality or a town.
        /// </summary>
        public AreaLevel Level { get; set; }

        /// <summary>
        /// District this row belongs to. Always filled.
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// Municipality this row belongs to. Empty on a district row.
        /// </summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// Town this row belongs to. Empty on a district or a municipality row.
        /// </summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// How many listings the price below was worked out from.
        /// Read this before the price: a median taken from three listings is not a market.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>
        /// Middle price for each square meter in this place.
        /// The middle value and not the average, so one very expensive villa cannot drag it up.
        /// </summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>
        /// Middle floor area in this place. Says what the stock here actually is: a town of
        /// studios and a town of villas can share a price for each square meter.
        /// </summary>
        public decimal MedianAreaM2 { get; set; }

        /// <summary>
        /// How many listings here are asking clearly less than the valuation model thinks they
        /// are worth. This is the "deals" number, and it means under-priced rather than cheap -
        /// a whole town being inexpensive is not a bargain, one flat below its own worth is.
        /// </summary>
        public int BelowEstimateCount { get; set; }

        /// <summary>
        /// Middle point of this place's listings, used to find which places border which.
        /// Null when none of the listings here carry coordinates.
        ///
        /// Worked out from the listings rather than from a real boundary, because we hold no
        /// polygons. Good enough to tell neighbours apart, not a substitute for a border.
        /// </summary>
        public decimal? CentroidLatitude { get; set; }

        /// <inheritdoc cref="CentroidLatitude"/>
        public decimal? CentroidLongitude { get; set; }

        /// <summary>
        /// How many listings here look like renovation projects, meaning the advert says it needs
        /// work or its energy certificate is D or worse. See the calculator for why the
        /// certificate is used: "needs renovation" alone was on 1.4% of stock, too few to measure.
        /// </summary>
        public int ProjectCount { get; set; }

        /// <summary>
        /// Of <see cref="ProjectCount"/>, how many were flagged by the advert itself saying it
        /// needs renovation. Kept apart from the energy-grade count because the two signals are
        /// not equally trustworthy: this one is whatever the agent felt like typing.
        /// </summary>
        public int ProjectByConditionCount { get; set; }

        /// <summary>
        /// Of <see cref="ProjectCount"/>, how many were flagged only by an energy certificate of
        /// D or worse. The more objective of the two signals, and roughly ten times as common.
        /// A place whose projects are all this kind is measuring "poorly insulated", which is
        /// related to "needs work" but is not the same thing - worth being able to see.
        /// </summary>
        public int ProjectByEnergyCount { get; set; }

        /// <summary>
        /// Middle price for each square meter of the project stock here.
        /// Null when there are too few projects to take a median from.
        /// </summary>
        public decimal? ProjectMedianPricePerM2 { get; set; }

        /// <summary>
        /// Middle floor area of the project stock. Without it the discount is a rate with nothing
        /// to multiply it by, and "€420/m² cheaper" never becomes a sum of money.
        /// </summary>
        public decimal? ProjectMedianAreaM2 { get; set; }

        /// <summary>
        /// The middle half of the project prices for each square meter: a quarter of them ask
        /// less than <see cref="ProjectP25PricePerM2"/>, a quarter more than
        /// <see cref="ProjectP75PricePerM2"/>.
        ///
        /// This is the number that decides whether the discount is real. Two medians always
        /// differ by something; if the project spread and the move-in spread sit on top of each
        /// other, that difference is noise wearing a decimal point.
        /// </summary>
        public decimal? ProjectP25PricePerM2 { get; set; }

        /// <inheritdoc cref="ProjectP25PricePerM2"/>
        public decimal? ProjectP75PricePerM2 { get; set; }

        /// <summary>How many listings here are ready to move into.</summary>
        public int MoveInCount { get; set; }

        /// <summary>
        /// Middle price for each square meter of the move-in-ready stock here, which is what the
        /// project stock is discounted against. Null when there is too little to compare with.
        /// </summary>
        public decimal? MoveInMedianPricePerM2 { get; set; }

        /// <inheritdoc cref="ProjectMedianAreaM2"/>
        public decimal? MoveInMedianAreaM2 { get; set; }

        /// <inheritdoc cref="ProjectP25PricePerM2"/>
        public decimal? MoveInP25PricePerM2 { get; set; }

        /// <inheritdoc cref="ProjectP25PricePerM2"/>
        public decimal? MoveInP75PricePerM2 { get; set; }

        /// <summary>
        /// Listings here that are neither a project nor clearly move-in ready - an unknown
        /// condition with no certificate to fall back on.
        ///
        /// They are counted and then left out of both sides, which is the honest thing to do and
        /// also the thing most worth showing: a place with 1,200 listings whose renovation
        /// discount rests on 40 projects and 300 finished homes has 860 listings with no opinion,
        /// and a reader who cannot see that will trust the discount more than it deserves.
        /// </summary>
        public int UnclassifiedCount { get; set; }

        /// <summary>
        /// One row per typology found here (T1, T2, T3...), for answering what a budget buys.
        /// </summary>
        public List<MarketAreaTypologyStats> TypologyStats { get; set; } = new();

        /// <summary>
        /// When these numbers were last worked out (UTC time).
        /// </summary>
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
