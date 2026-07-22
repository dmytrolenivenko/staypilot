
namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// The field used to sort a list of properties.
    /// </summary>
    public enum SortBy
    {
        Id, // Sort by database Id.
        Price, // Sort by asking price.
        PricePerM2, // Sort by price for each square meter.
        AreaM2, // Sort by floor area.
        CreatedAtUtc, // Sort by the date we saved it.
        DistanceToBeachMeters // Sort by distance to the nearest beach.
    }
}
