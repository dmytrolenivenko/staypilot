using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for the market overview: what one slice of the market is asking, right now.
    ///
    /// Read <see cref="ListingCount"/> first. Everything below it is worked out from that many
    /// listings, and a median taken from four adverts is four adverts, not a market.
    /// </summary>
    public class MarketOverviewResponse : ResponseBase
    {
        /// <summary>
        /// The slice written out for a human: "Guia (Albufeira)", or "All areas" when nothing
        /// was narrowed. Built on the server so every screen names a place the same way.
        /// </summary>
        public string PlaceName { get; set; } = string.Empty;

        /// <summary>
        /// How many listings the numbers were worked out from. Zero is a valid answer, not an
        /// error: nothing here matches what you asked for.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>Asking price, in euros.</summary>
        public MarketOverviewStats Price { get; set; } = new();

        /// <summary>
        /// Asking price for each square meter. The one number that compares across places, since
        /// it takes the size of the stock out of the picture.
        /// </summary>
        public MarketOverviewStats PricePerM2 { get; set; } = new();

        /// <summary>Floor area, in square meters. Says what kind of stock this slice is.</summary>
        public MarketOverviewStats AreaM2 { get; set; } = new();

        /// <summary>
        /// The price distribution, cheapest bar first. Empty when nothing matched.
        /// The shape matters: an average sitting between two clusters describes neither of them.
        /// </summary>
        public List<MarketOverviewPriceBucketResponse> Distribution { get; set; } = new();

        /// <summary>
        /// One row per room layout found in this slice, so you can see what is actually on sale
        /// here and not only what the whole slice averages out to. Empty when nothing matched.
        /// </summary>
        public List<MarketOverviewTypologyResponse> Typologies { get; set; } = new();

        /// <summary>
        /// The same slice cut one level finer: districts when nothing was narrowed, municípios
        /// inside a chosen district, freguesias inside a chosen município.
        ///
        /// Null only when the slice is already a freguesia — the finest grain we hold — or when
        /// nothing matched. A single median over every district in the country describes no
        /// market anybody can buy in, so the broader the slice, the more this is the real answer
        /// and the summary above is only the header.
        /// </summary>
        public MarketOverviewBreakdownResponse? Breakdown { get; set; }

        /// <summary>
        /// When these numbers were worked out (UTC time), which is when you asked for them.
        /// Unlike the leaderboard, nothing here is precomputed, so it is never stale.
        /// </summary>
        public DateTime GeneratedAtUtc { get; set; }
    }

    /// <summary>
    /// One measured quantity summarised four ways.
    ///
    /// <see cref="Median"/> and <see cref="Average"/> are both here on purpose: the gap between
    /// them is the reading. They agree on an even market, and the average runs away from the
    /// median exactly where a few very expensive listings are pulling it.
    /// </summary>
    public class MarketOverviewStats
    {
        /// <summary>Middle value. The one to quote - one villa cannot drag it up.</summary>
        public decimal Median { get; set; }

        /// <summary>Plain mean of every listing, outliers included.</summary>
        public decimal Average { get; set; }

        /// <summary>Lowest value found. A single advert, so read it as one, not as the market.</summary>
        public decimal Min { get; set; }

        /// <inheritdoc cref="Min"/>
        public decimal Max { get; set; }
    }

    /// <summary>
    /// One bar of the price distribution: how many listings ask between two prices.
    /// </summary>
    public class MarketOverviewPriceBucketResponse
    {
        /// <summary>Lowest price in this bar.</summary>
        public decimal FromPrice { get; set; }

        /// <summary>Highest price in this bar.</summary>
        public decimal ToPrice { get; set; }

        /// <summary>How many listings fall in it.</summary>
        public int ListingCount { get; set; }

        /// <summary>Its share of all the listings in the slice, as a percentage.</summary>
        public decimal SharePercent { get; set; }
    }

    /// <summary>
    /// What one room layout costs in this slice.
    /// </summary>
    public class MarketOverviewTypologyResponse
    {
        /// <summary>How many rooms, Portuguese T-style (T0, T1, T2...).</summary>
        public Typology Typology { get; set; }

        /// <summary>How many listings of this layout the numbers came from.</summary>
        public int ListingCount { get; set; }

        /// <summary>Middle asking price for this layout here.</summary>
        public decimal MedianPrice { get; set; }

        /// <summary>Middle floor area, so the price reads as space and not only as bedrooms.</summary>
        public decimal MedianAreaM2 { get; set; }

        /// <summary>Middle price for each square meter for this layout here.</summary>
        public decimal MedianPricePerM2 { get; set; }
    }
}
