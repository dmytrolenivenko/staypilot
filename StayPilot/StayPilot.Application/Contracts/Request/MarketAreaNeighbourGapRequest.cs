using System.ComponentModel.DataAnnotations;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request for pairs of nearby places with a big price gap between them.
    /// </summary>
    public class MarketAreaNeighbourGapRequest
    {
        /// <inheritdoc cref="MarketAreaLeaderboardRequest.Level"/>
        public AreaLevel Level { get; set; } = AreaLevel.Municipality;

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.MinListings"/>
        [Range(1, 1000)]
        public int MinListings { get; set; } = 5;

        /// <summary>
        /// How close two places have to be to count as neighbours, in kilometres.
        /// Twenty-five by default: far enough to pair a town with the next one along, close
        /// enough that the pair is still a real choice rather than two ends of the country.
        /// </summary>
        [Range(1, 500)]
        public int MaxDistanceKm { get; set; } = 25;

        /// <summary>
        /// The smallest gap worth reporting, as a percentage. Two places 4% apart is noise.
        /// </summary>
        [Range(1, 100)]
        public int MinGapPercent { get; set; } = 20;
    }
}
