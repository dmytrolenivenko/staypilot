using StayPilot.Domain.Enums;

namespace StayPilot.Application.ReadModels
{
    /// <summary>
    /// One active listing cut down to what it takes to decide whether it is a deal: where it is,
    /// what condition it is in, and what it asks per square meter.
    ///
    /// Top Deals grades every active listing in the country and keeps ten of them. Reading whole
    /// entities to do that meant loading tens of thousands of listings with their market areas
    /// and snapshots attached, then throwing all but ten away. The grading runs on these instead,
    /// and only the ten that win are read back in full.
    /// </summary>
    public readonly record struct TopDealCandidate(
        int ListingId,
        PropertyCondition Condition,
        string? EnergyCertificate,
        string District,
        string Municipality,
        string Town,
        decimal PricePerM2);
}
