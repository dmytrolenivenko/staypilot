using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response.SubResponse
{
    /// <summary>
    /// How keen buyers are in the place a property sits in, and the working behind it.
    ///
    /// Everything here is measured from adverts, never from sales - there are no sale prices in
    /// this database. Read <see cref="IsMeasurable"/> before <see cref="Level"/>: when it is false
    /// the level means "not measured", not "average".
    /// </summary>
    public class AreaDemandResponse
    {
        /// <summary>The word shown on screen: Cold, Soft, Balanced, Firm or Hot.</summary>
        public DemandLevel Level { get; set; }

        /// <summary>The score behind the word, 0-100. Zero when nothing could be measured.</summary>
        public decimal Score { get; set; }

        /// <summary>False when neither half of the score could be worked out.</summary>
        public bool IsMeasurable { get; set; }

        /// <summary>The place these numbers describe, in words.</summary>
        public string PlaceName { get; set; } = string.Empty;

        /// <summary>Middle number of days a home sits here. Null when it could not be measured.</summary>
        public decimal? MedianDaysOnMarket { get; set; }

        /// <summary>
        /// True when the days above were measured on homes that actually sold, false when they are
        /// how long live adverts have been up so far - which is a floor, not a duration.
        /// </summary>
        public bool DaysMeasuredOnSold { get; set; }

        /// <summary>The days-on-market half of the score, 0-100. Null when it was not measured.</summary>
        public decimal? DaysScore { get; set; }

        /// <summary>New adverts first seen in the last 90 days.</summary>
        public int NewListingsRecent { get; set; }

        /// <summary>New adverts first seen in the 90 days before those.</summary>
        public int NewListingsPrevious { get; set; }

        /// <summary>How much new supply changed between those two windows, in percent.</summary>
        public decimal? SupplyChangePercent { get; set; }

        /// <summary>The supply half of the score, 0-100. Null when it was not measured.</summary>
        public decimal? SupplyScore { get; set; }

        /// <summary>How many listings in this place the score rests on.</summary>
        public int SampleSize { get; set; }

        /// <summary>How many days of history we hold for this place. Bounds both measurements.</summary>
        public int CollectionSpanDays { get; set; }

        /// <summary>What was measured and what was not, in a sentence.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
