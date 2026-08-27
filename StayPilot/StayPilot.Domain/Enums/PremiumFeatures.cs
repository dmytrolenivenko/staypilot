
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
        /// RETIRED - replaced by <see cref="CloseToBeach"/>. Its premium was read "per halving of
        /// the distance to the beach", which was correct and unreadable: nobody could tell what
        /// it meant for their own flat without doing logarithms.
        ///
        /// Kept only so rows written before the switch still read back. Nothing produces it any
        /// more, and the first recalculation clears the last of them.
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
        HasBalcony = 17,

        /// <summary>
        /// Within walking distance of the sea - 500m or less, per
        /// <c>ValuationSubject.CloseToBeachMeters</c>. A plain yes/no like a garage: either the
        /// flat is close to the beach or it is not.
        ///
        /// Note this is the reported premium only. The price estimate itself still uses the
        /// exact distance on a smooth curve, so nothing is rounded off where the € figure is
        /// decided - see <c>ValuationModel.BuildRow</c>.
        /// </summary>
        CloseToBeach = 18
    }
}
