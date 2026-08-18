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

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.District"/>
        [StringLength(100)]
        public string? District { get; set; }

        /// <inheritdoc cref="MarketAreaLeaderboardRequest.Municipality"/>
        [StringLength(100)]
        public string? Municipality { get; set; }

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

        /// <summary>
        /// Compare the two places on this typology's price for each square meter instead of on
        /// all their stock at once. Empty compares everything, which is the default.
        ///
        /// This is the filter that makes a gap trustworthy. Two places can differ by 30% on all
        /// stock purely because one of them sells villas and the other sells studios - the gap is
        /// then a fact about the buildings, not about the places, and moving would not save you
        /// anything. Comparing T2 against T2 takes that away.
        /// </summary>
        public Typology? Typology { get; set; }

        /// <summary>
        /// The fewest listings of the chosen <see cref="Typology"/> a place must have before it
        /// can be half of a pair. Ignored when no typology was chosen.
        ///
        /// Separate from <see cref="MinListings"/>, which gates the place as a whole: a município
        /// with 400 listings can still have three T4s, and a "T4 gap" measured off three adverts
        /// is exactly the kind of finding this screen should not be producing.
        /// </summary>
        [Range(1, 1000)]
        public int MinTypologyListings { get; set; } = 5;
    }
}
