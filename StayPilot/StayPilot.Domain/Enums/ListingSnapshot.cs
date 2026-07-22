
namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// State of a listing on the day we checked it.
    /// </summary>
    public enum ListingStatus
    {
        Sold = 1, // The property was sold and is no longer for sale.
        Active = 2, // The property is still for sale.
        PriceChanged = 3 // The price changed since the last check.
    }
}
