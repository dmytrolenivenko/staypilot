
namespace StayPilot.Domain.Enums
{
    /// <summary>
    /// The state of a property, from bad to brand new.
    /// </summary>
    public enum PropertyCondition
    {
        Unknown = 1, // We do not know the state.
        NeedsRenovation = 2, // Needs repair work.
        Used = 3, // Lived in, normal wear.
        Good = 4, // In good shape.
        Renovated = 5, // Repaired or upgraded recently.
        NewBuild = 6 // Brand new, never used.
    }
}
