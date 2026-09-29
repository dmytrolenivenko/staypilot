namespace StayPilot.Api
{
    /// <summary>
    /// The output-cache policies this API defines, named in one place so Program.cs and the
    /// [OutputCache] attributes on the controllers cannot drift apart over a typo.
    /// </summary>
    public static class OutputCachePolicies
    {
        /// <summary>
        /// For the public market answers: the overview, the leaderboards, what a budget reaches,
        /// the neighbour gaps, the top deals. Every caller gets the same numbers for the same
        /// question, so one caller's answer is every caller's answer.
        ///
        /// Never put this on anything that reads the signed-in user. This policy deliberately
        /// caches for signed-in callers too (see <see cref="PublicMarketDataCachePolicy"/>), so
        /// on a per-user endpoint one person's answer would be served to the next person who
        /// asks. OwnedProperty, the valuations and the investment analysis are all out of bounds.
        /// </summary>
        public const string PublicMarketData = "public-market-data";

        /// <summary>
        /// How long a public market answer is held.
        ///
        /// Short on purpose. These numbers change only when a scrape imports and the stats are
        /// recalculated, so this could be hours - but nothing here evicts the cache when that
        /// happens, and five minutes is a stale window worth nobody's attention. Long enough
        /// that clicking around the site re-reads almost nothing, short enough that a
        /// recalculation shows up while you are still looking at the screen.
        /// </summary>
        public const int CacheMinutes = 5;
    }
}
