namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// The last priced result for one owned property - a cache, not a history. Revaluing a
    /// property overwrites its row here rather than adding a new one, and a property with no row
    /// at all has simply never been valued yet.
    ///
    /// This exists because pricing a portfolio fits the valuation model over every listing in the
    /// database and then runs a comp search per property - fine once, too slow to redo on every
    /// visit to the Valuation screen. The screen reads this table; only the explicit "Re-price"
    /// action recomputes and writes it.
    /// </summary>
    public class OwnedPropertyValuation
    {
        /// <summary>
        /// Id of the owned property this valuation is for. Also the primary key: one row per
        /// property, sharing its key rather than carrying an identity of its own.
        /// </summary>
        public int OwnedPropertyId { get; set; }

        /// <summary>The property this valuation is for.</summary>
        public OwnedProperty OwnedProperty { get; set; } = null!;

        /// <summary>
        /// Everything the last revaluation computed for this property - place, price, confidence,
        /// demand, forecast - serialized as JSON. Kept as one blob rather than a column per field
        /// (or child tables for the nested demand/forecast blocks) because it is never queried by
        /// its contents: every read either wants the whole thing or nothing.
        /// </summary>
        public string ResultJson { get; set; } = string.Empty;

        /// <summary>When this row was last written by a revaluation.</summary>
        public DateTime ValuatedAtUtc { get; set; }
    }
}
