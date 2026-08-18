using System.ComponentModel.DataAnnotations;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request for what a budget buys in each place.
    /// </summary>
    public class MarketAreaBudgetRequest
    {
        /// <summary>How much there is to spend, in euros.</summary>
        [Range(1000, 100000000)]
        public decimal Budget { get; set; } = 300000;

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.Level"/>
        public AreaLevel Level { get; set; } = AreaLevel.Municipality;

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.MinListings"/>
        [Range(1, 1000)]
        public int MinListings { get; set; } = 5;

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.District"/>
        [StringLength(100)]
        public string? District { get; set; }

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.Municipality"/>
        [StringLength(100)]
        public string? Municipality { get; set; }

        /// <summary>
        /// Leave out places where the budget does not reach at least this many rooms.
        ///
        /// "What does 300k buy" is only half a question when a T1 in Lisboa and a T4 in Beja both
        /// answer it. Set a floor and the board becomes "where does 300k buy me the T3 I need",
        /// which has far fewer answers and is the one worth acting on. Empty means no floor.
        /// </summary>
        public Typology? MinTypology { get; set; }

        /// <summary>
        /// How much the budget may be stretched past <see cref="Budget"/>, as a percentage, when
        /// working out what it reaches.
        ///
        /// Zero by default, so nothing over budget is ever shown as affordable. Raised, it
        /// answers the question people actually have at the edge of their means: "and what would
        /// another 10% get me". Places reached only by the stretch are flagged in the response,
        /// never quietly mixed in with the ones that fit.
        /// </summary>
        [Range(0, 100)]
        public int StretchPercent { get; set; }
    }
}
