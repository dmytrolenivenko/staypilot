using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for neighbour gaps: pairs of nearby places where one is much dearer than the
    /// other, biggest gap first.
    /// </summary>
    public class MarketAreaNeighbourGapResponse : ResponseBase
    {
        public List<NeighbourGapResponse> Items { get; set; } = new();

        /// <summary>When the numbers were last worked out. Null while the table is empty.</summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }

    /// <summary>
    /// Two nearby places and the price gap between them.
    /// </summary>
    public class NeighbourGapResponse
    {
        /// <summary>The dearer of the two.</summary>
        public string ExpensivePlace { get; set; } = string.Empty;

        public decimal ExpensivePricePerM2 { get; set; }

        public int ExpensiveListingCount { get; set; }

        /// <summary>The cheaper of the two - the one you would move to.</summary>
        public string CheaperPlace { get; set; } = string.Empty;

        public decimal CheaperPricePerM2 { get; set; }

        public int CheaperListingCount { get; set; }

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
}
