namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// The column used to sort a list of market areas.
    /// </summary>
    public enum MarketAreaSortBy
    {
        Location, // District, then municipality, then town: the order the table reads in.
        Id, // Sort by database Id.
        District, // Sort by district name.
        Municipality, // Sort by municipality name.
        Town, // Sort by town name.
        Zone, // Sort by zone name. Rows without a zone come first.
        Country, // Sort by country name.
        Notes // Sort by the free text notes. Rows without notes come first.
    }
}
