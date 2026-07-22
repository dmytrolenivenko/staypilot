using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response.SubResponse
{
    public class ValuationComp
    {
        public int AreaM2 { get; set; }  // size, to compare to yours
        public decimal PricePerM2 { get; set; }  // the number that drives the estimate
        public int? DistanceToBeachMeters { get; set; }  // how beach-comparable it is
        public Typology Typology { get; set; }  // T1/T2… (enum you already have)
        public DateTime SnapshotDateUtc { get; set; }  // FRESHNESS — how old is this comp
    }
}
