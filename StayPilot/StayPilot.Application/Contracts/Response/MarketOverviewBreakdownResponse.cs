using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// The slice broken into the places inside it.
    ///
    /// The summary above it answers "what does this cost here"; a slice as broad as a whole
    /// country, or even a whole district, has no single answer to that. This is where the answer
    /// actually lives: one row per place, each with the evidence behind it, so a median worked
    /// out over 4,000 listings is never read as if it described any one town.
    /// </summary>
    public class MarketOverviewBreakdownResponse
    {
        /// <summary>
        /// Which grain the rows measure. Always one step finer than the slice that was asked for.
        /// </summary>
        public AreaLevel Level { get; set; }

        /// <summary>
        /// The places inside the slice, dearest per square meter first. Every place is here -
        /// paging or trimming happens in the browser, where the reader can undo it.
        /// </summary>
        public List<MarketOverviewBreakdownItemResponse> Items { get; set; } = new();
    }

    /// <summary>
    /// One place inside the slice, measured the same way the slice itself was.
    /// </summary>
    public class MarketOverviewBreakdownItemResponse
    {
        public string District { get; set; } = string.Empty;

        /// <summary>Empty on a district row.</summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>Empty on a district or a município row.</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>
        /// The place written out for a human, matching how every other screen names one.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// How many of the slice's listings sit here. Read it before the medians beside it.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>This place's share of the whole slice, as a percentage.</summary>
        public decimal SharePercent { get; set; }

        public decimal MedianPrice { get; set; }

        public decimal MedianAreaM2 { get; set; }

        /// <summary>The number that compares across places, since it takes size out of it.</summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>
        /// How far this place's price per square meter sits from the slice's own median, as a
        /// percentage. Positive is dearer than the slice it belongs to.
        ///
        /// This is the column the screen exists for: "Albufeira is 34% above the Faro median" is
        /// a finding; "Albufeira is €2,410/m²" is a number you then have to do arithmetic on.
        /// </summary>
        public decimal VsSlicePercent { get; set; }
    }
}
