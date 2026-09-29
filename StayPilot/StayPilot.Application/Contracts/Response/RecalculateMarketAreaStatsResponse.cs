using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// A recalculation: what the run did, and where it has got to.
    ///
    /// The run outlives the request that starts it - it reads every listing we hold and rewrites
    /// the whole stats table, which takes minutes at national size, and Azure closes an HTTP
    /// request at around 230 seconds whatever the caller is willing to wait. So starting one
    /// answers with <see cref="IsRunning"/> set and the counts still empty, and the caller polls
    /// for the same shape until it stops.
    ///
    /// The state half lives in the API's memory, so a restart forgets it. The durable answer to
    /// "are the numbers fresh" is the CalculatedAtUtc stamped on the stats rows, which every
    /// leaderboard read returns - compare that before and after rather than trusting this to
    /// survive a recycle.
    /// </summary>
    public class RecalculateMarketAreaStatsResponse : ResponseBase
    {
        /// <summary>How many listings had a usable price and could be placed.</summary>
        public int ListingsUsed { get; set; }

        /// <summary>
        /// How many rows were written, counting all three levels together.
        /// </summary>
        public int RowsWritten { get; set; }

        /// <summary>
        /// The stamp the run put on its rows (UTC time). Null until one has finished - which is
        /// also how a caller tells a run that landed from one that stopped on the way.
        /// </summary>
        public DateTime? CalculatedAtUtc { get; set; }

        /// <summary>True while a run is going on right now.</summary>
        public bool IsRunning { get; set; }

        /// <summary>When the current - or most recent - run started. Null when none has.</summary>
        public DateTime? StartedAtUtc { get; set; }

        /// <summary>
        /// When the most recent run stopped, whether it worked or not. Null while one is still
        /// going, and null when none has run in this process.
        /// </summary>
        public DateTime? FinishedAtUtc { get; set; }

        /// <summary>
        /// Why the last run did not work, or null when it did. Carries the error the
        /// recalculation itself reported, or the exception that stopped it - a run that dies has
        /// to say so somewhere, or the caller sees "not running" and reads it as "finished".
        /// </summary>
        public string? FailureReason { get; set; }
    }
}
