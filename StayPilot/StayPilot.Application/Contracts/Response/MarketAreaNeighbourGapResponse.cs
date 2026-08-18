using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for neighbour gaps: pairs of nearby places where one is much dearer than the
    /// other, biggest gap first.
    /// </summary>
    public class MarketAreaNeighbourGapResponse : ResponseBase
    {
        public List<NeighbourGapResponse> Items { get; set; } = new();

        /// <summary>
        /// The typology every pair was compared on, echoed back. Null means all stock at once.
        ///
        /// Worth showing on the screen: a 30% gap between T2s and a 30% gap between "everything"
        /// are different claims, and the second one can be entirely explained by one place
        /// selling villas while the other sells studios.
        /// </summary>
        public Typology? ComparedOn { get; set; }

        /// <summary>When the numbers were last worked out. Null while the table is empty.</summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// Two nearby places and the price gap between them.
    /// </summary>
    public class NeighbourGapResponse
    {
        /// <summary>The dearer of the two.</summary>
        public NeighbourGapPlaceResponse Expensive { get; set; } = new();

        /// <summary>The cheaper of the two - the one you would move to.</summary>
        public NeighbourGapPlaceResponse Cheaper { get; set; } = new();

        /// <summary>
        /// How far apart the two places are, in kilometres. Measured between the middle points of
        /// their listings, not between real borders - we hold no boundaries. So read it as
        /// roughly how far you would move, not as an exact distance.
        /// </summary>
        public decimal DistanceKm { get; set; }

        /// <summary>
        /// How much cheaper the cheaper place is, as a percentage of the dearer one.
        /// 37 means you pay 37% less for a square meter by moving.
        /// </summary>
        public decimal GapPercent { get; set; }
    }

    /// <summary>
    /// One half of a pair, with the place broken into its parts.
    ///
    /// It used to be a single string - "Guia (Albufeira)" - which left the reader guessing what
    /// the bracket held, and whether the name in front of it was a município or a freguesia. The
    /// parts are sent separately now so the screen can say which grain it is showing and spell
    /// the parents out; <see cref="DisplayName"/> stays for the places that need one line.
    /// </summary>
    public class NeighbourGapPlaceResponse
    {
        /// <summary>Which grain this place is. The same for both halves of any one pair.</summary>
        public AreaLevel Level { get; set; }

        public string District { get; set; } = string.Empty;

        /// <summary>Empty on a district row.</summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>Empty on a district or a município row.</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>The place on one line, the way every other screen names one.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// The price the gap was worked out from. All this place's stock normally; one typology's
        /// stock when the caller narrowed the comparison to a typology.
        /// </summary>
        public decimal MedianPricePerM2 { get; set; }

        /// <summary>How many listings this side's price rests on, on the same basis.</summary>
        public int ListingCount { get; set; }

        /// <summary>
        /// This place's median across all its stock, whatever the comparison ran on.
        ///
        /// Identical to <see cref="MedianPricePerM2"/> unless a typology was chosen, and then the
        /// two together are the finding: "T2s here are 30% dearer, but all stock is only 4%
        /// dearer" says the gap is about the flats, not about the place.
        /// </summary>
        public decimal AllStockPricePerM2 { get; set; }

        /// <inheritdoc cref="AllStockPricePerM2"/>
        public int AllStockListingCount { get; set; }
    }
}
