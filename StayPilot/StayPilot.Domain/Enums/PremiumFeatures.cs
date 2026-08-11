
namespace StayPilot.Domain.Enums
{
    public enum PremiumFeatures
    {
        HasSeaView = 1,
        HasCityView = 2,
        HasGarage = 3,
        HasSwimmingPool = 4,
        HasTerrace = 5,
        HasElevator = 6,
        HasAirConditioning = 7,
        IsFurnished = 8,
        HasParking = 9,
        IsNewBuild = 10,
        IsRenovated = 11,

        /// <summary>
        /// How much closer to the beach is worth. Unlike everything else here this is not a
        /// yes/no feature, so its premium is read as "per halving of the distance to the beach"
        /// - being 500m away instead of 1km. Callers showing this to a user must say so.
        /// </summary>
        BeachProximity = 12
    }
}
