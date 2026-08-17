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
    }
}
