
using System.ComponentModel.DataAnnotations;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request for the best-priced active listings in one place: the ones asking the most below
    /// their own town's median euro per square meter. Every filter is optional - an empty request
    /// ranks the whole country.
    /// </summary>
    public class TopDealsRequest
    {
        /// <summary>Filter by district (address part).</summary>
        [StringLength(100)]
        public string? District { get; set; }

        /// <summary>Filter by municipality (address part).</summary>
        [StringLength(100)]
        public string? Municipality { get; set; }

        /// <summary>Filter by town (address part).</summary>
        [StringLength(100)]
        public string? Town { get; set; }

        /// <summary>Filter by zone inside the town. Many listings have no zone.</summary>
        [StringLength(100)]
        public string? Zone { get; set; }

        /// <summary>
        /// Keep only listings in this state (for example move-in ready or needs renovation).
        /// Null keeps both - a renovation project is still graded fairly, against the median of
        /// other projects rather than of move-in-ready stock, so the two never get mixed up.
        /// </summary>
        public PropertyCondition? Condition { get; set; }

        /// <summary>How many deals to return, best first. Ten by default.</summary>
        [Range(1, 50)]
        public int Count { get; set; } = 10;
    }
}
