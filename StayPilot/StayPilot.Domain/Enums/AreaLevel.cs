
namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// How wide one row of market area stats is. A district holds municipalities,
    /// a municipality holds towns.
    ///
    /// Zones are left out on purpose: too few listings in one zone to trust a median.
    /// </summary>
    public enum AreaLevel
    {
        District = 1, // The largest area inside the country.
        Municipality = 2, // A smaller area inside a district.
        Town = 3 // One town (freguesia) inside a municipality.
    }
}
