using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Runs the market area stats recalculation away from the request that asked for it.
    ///
    /// It exists because the recalculation outlives an HTTP request: at national size the fit and
    /// the rewrite take minutes, and Azure closes the connection at around 230 seconds with a 504
    /// whatever the client's own timeout says. Starting the work and answering immediately is what
    /// makes the endpoint usable again; nothing here makes the work itself faster.
    ///
    /// The implementation lives in the Api project alongside <see cref="ICurrentUser"/>, for the
    /// same reason: it needs the host's DI scoping to outlive the request, which is an ASP.NET
    /// Core concern rather than an Application one.
    /// </summary>
    public interface IStatsRecalculationRunner
    {
        /// <summary>
        /// Starts a run unless one is already going, and reports the state either way. A second
        /// caller never starts a second run: both would delete the whole stats table and write it
        /// back, and whichever finished last would win a race nobody can see.
        /// </summary>
        RecalculateMarketAreaStatsResponse Start();

        /// <summary>How the current or most recent run is doing. Safe to call as often as you like.</summary>
        RecalculateMarketAreaStatsResponse GetStatus();
    }
}
