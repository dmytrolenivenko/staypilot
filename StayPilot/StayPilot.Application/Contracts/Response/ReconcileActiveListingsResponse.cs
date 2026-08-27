using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// What a reconciliation call found and changed.
    /// </summary>
    public class ReconcileActiveListingsResponse : ResponseBase
    {
        /// <summary>How many listings this API held as Active before the call.</summary>
        public int ActiveListingsChecked { get; set; }

        /// <summary>How many of them were missing from ActiveUrls and got a new Sold snapshot.</summary>
        public int MarkedSoldCount { get; set; }

        /// <summary>The URLs that got marked sold, so the caller can log it without a second call.</summary>
        public List<string> MarkedSoldUrls { get; set; } = new();
    }
}
