namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// How specifically the valuation model was able to place a property before pricing it.
    ///
    /// The model prices a property as the most specific place it has enough listings to describe,
    /// falling back a level at a time when it does not. This records where that fall stopped, and
    /// it is the honest measure of how well the model knows a property's market - separate from
    /// how close the nearest advert happens to be.
    ///
    /// Ordered least to most specific, so a caller can compare with &gt; and &lt;.
    /// </summary>
    public enum LocationPrecision
    {
        /// <summary>
        /// Nowhere local had enough listings, so the property was priced off the national
        /// average. Whatever else is true of the estimate, the model does not know this market.
        /// </summary>
        National = 0,

        /// <summary>Priced as its district - one lump covering coast and interior alike.</summary>
        District = 1,

        /// <summary>Priced as its município. The level INE publishes sale prices at.</summary>
        Municipality = 2,

        /// <summary>Priced as its own zone, which is as specific as this data goes.</summary>
        Area = 3,
    }
}
