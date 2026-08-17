
using StayPilot.Domain.Enums;

namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// What one typology costs in one place: the T2 row for Albufeira, the T3 row for Olhão.
    ///
    /// This is what makes "what does €300,000 buy me here" answerable. The parent row's single
    /// median cannot do it: it mixes studios in with villas, so it says what a square meter costs
    /// but not what you actually get for your money.
    /// </summary>
    public class MarketAreaTypologyStats
    {
        /// <summary>
        /// Database Id for this row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Id of the place these numbers belong to.
        /// </summary>
        public int MarketAreaStatsId { get; set; }

        /// <summary>
        /// The place these numbers belong to.
        /// </summary>
        public MarketAreaStats MarketAreaStats { get; set; } = null!;

        /// <summary>
        /// How many rooms, Portuguese T-style (T0, T1, T2...).
        /// </summary>
        public Typology Typology { get; set; }

        /// <summary>
        /// How many listings of this typology the numbers below were worked out from.
        /// </summary>
        public int ListingCount { get; set; }

        /// <summary>
        /// Middle asking price for this typology here. This is the number a budget is compared
        /// against - what a T2 in this place actually costs.
        /// </summary>
        public decimal MedianPrice { get; set; }

        /// <summary>
        /// Middle floor area for this typology here, so a budget answer can say how much space
        /// the money buys and not only how many bedrooms.
        /// </summary>
        public decimal MedianAreaM2 { get; set; }

        /// <summary>
        /// Middle price for each square meter for this typology here.
        /// </summary>
        public decimal MedianPricePerM2 { get; set; }
    }
}
