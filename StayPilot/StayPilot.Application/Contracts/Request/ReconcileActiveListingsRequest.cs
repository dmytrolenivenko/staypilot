namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Every listing URL a fresh sweep actually saw as still live, from wherever it swept.
    /// Any listing this API holds as Active whose URL is missing from this list gets a new
    /// Sold snapshot - see ListingSnapshotController.ReconcileActiveListingsAsync.
    /// </summary>
    public class ReconcileActiveListingsRequest
    {
        /// <summary>URLs seen live in the sweep this call reports on.</summary>
        public List<string> ActiveUrls { get; set; } = new();
    }
}
