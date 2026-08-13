namespace StayPilot.Application.Contracts.Response
{
    public class BulkAddPropertyListingResponse
    {
        public int TotalReceived { get; set; }

        public int TotalAdded { get; set; }

        public int SnapShotUpdated { get; set; }

        public int Unchanged { get; set; }

        public Dictionary<string, string> FailedListings { get; set; }
    }
}
