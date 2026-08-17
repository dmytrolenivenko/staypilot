using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// What happened to a bulk upload: how many listings landed in each outcome.
    /// The listings that did not make it are in Errors, one entry each, with the source url
    /// inside the message so the caller knows which one it is.
    /// </summary>
    public class BulkAddPropertyListingResponse : ResponseBase
    {
        public int TotalReceived { get; set; }

        public int TotalAdded { get; set; }

        public int SnapShotUpdated { get; set; }

        public int Unchanged { get; set; }

        /// <summary>How many listings were rejected or could not be saved.</summary>
        public int TotalFailed => Errors?.Count ?? 0;
    }
}
