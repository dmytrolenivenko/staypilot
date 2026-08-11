
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
        BeachProximity = 12,

        /// <summary>
        /// What one step up the energy certificate scale is worth (G -> F -> E ... -> A+).
        /// Not a yes/no feature: the premium is per step.
        /// </summary>
        EnergyGrade = 13,

        /// <summary>What one more bathroom is worth. Per bathroom, not yes/no.</summary>
        ExtraBathroom = 14,

        /// <summary>What being one storey higher up is worth. Per floor, not yes/no.</summary>
        FloorLevel = 15,

        /// <summary>A property the advert flags as needing renovation work. Yes/no.</summary>
        NeedsRenovation = 16,

        /// <summary>What one balcony is worth. Per balcony, not yes/no.</summary>
        HasBalcony = 17
    }
}
