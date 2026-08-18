namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// How keen buyers are in one place, on a five step scale.
    ///
    /// Scored from two things only: how long homes sit on the market, and whether new adverts
    /// are arriving faster than they used to. Both come from adverts, so this measures interest
    /// in a place, not sales - we have no sale prices and never claim to.
    /// </summary>
    public enum DemandLevel
    {
        /// <summary>Homes sit for a long time and new supply is piling up.</summary>
        Cold = 1,

        /// <summary>Slower than typical, more supply arriving than before.</summary>
        Soft = 2,

        /// <summary>Nothing unusual in either direction.</summary>
        Balanced = 3,

        /// <summary>Selling faster than typical, or supply thinning out.</summary>
        Firm = 4,

        /// <summary>Homes go quickly and there is less new stock than there was.</summary>
        Hot = 5,
    }
}
