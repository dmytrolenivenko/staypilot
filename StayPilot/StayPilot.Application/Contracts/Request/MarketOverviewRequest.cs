using StayPilot.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request for the market overview: pick a place, optionally narrow it to one kind of
    /// property, and read back what that slice of the market is asking.
    ///
    /// Every filter is optional, so sending nothing measures the whole dataset. That is a real
    /// answer but a broad one - a median taken across every district at once describes no market
    /// anybody can buy in, which is why the screen starts you on a district.
    ///
    /// Unlike the leaderboard, this is worked out from the listings on each call rather than read
    /// from the stats table: the point of the screen is an arbitrary slice (this town, T2 only,
    /// apartments only), and no precomputed table can hold every combination of those.
    /// </summary>
    public class MarketOverviewRequest
    {
        /// <summary>Keep only listings in this district. Empty means every district.</summary>
        [StringLength(100)]
        public string? District { get; set; }

        /// <summary>Keep only listings in this municipality. Empty means every one in the district.</summary>
        [StringLength(100)]
        public string? Municipality { get; set; }

        /// <summary>Keep only listings in this town. Empty means every town in the municipality.</summary>
        [StringLength(100)]
        public string? Town { get; set; }

        /// <summary>Kind of property (apartment, villa, house, land). Empty means all kinds.</summary>
        public PropertyType? PropertyType { get; set; }

        /// <summary>Room layout (T0, T1, T2...). Empty means all layouts.</summary>
        public Typology? Typology { get; set; }

        /// <summary>
        /// How many bars the price distribution is cut into. Ten by default: enough to show the
        /// shape of the market, few enough that each bar still holds listings worth counting.
        /// </summary>
        [Range(4, 20)]
        public int BucketCount { get; set; } = 10;
    }
}
