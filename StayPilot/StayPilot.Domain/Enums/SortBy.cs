namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// The field used to sort a list of properties.
    /// New values go on the end: callers (including the separate scraper repo) key on the
    /// number, so nothing above may be renumbered or reused.
    /// </summary>
    public enum SortBy
    {
        Id, // Sort by database Id.
        Price, // Sort by asking price.
        PricePerM2, // Sort by price for each square meter.
        AreaM2, // Sort by floor area.
        CreatedAtUtc, // Sort by the date we saved it.
        DistanceToBeachMeters, // Sort by distance to the nearest beach.
        Location, // Sort by place name: distrito, then municipio, then freguesia.
        PropertyType, // Sort by kind of property.
        Typology, // Sort by room layout (T0, T1, T2...).
        ListingStatus // Sort by the newest snapshot's status.
    }
}
