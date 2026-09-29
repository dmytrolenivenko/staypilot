using StayPilot.Domain.Enums;

namespace StayPilot.Application.ReadModels
{
    /// <summary>
    /// One listing cut down to what the overview measures, and nothing else.
    ///
    /// The repository reads this shape straight out of SQL rather than handing the overview whole
    /// <see cref="Domain.Entities.PropertyListing"/> entities: the screen needs seven numbers per
    /// listing and an entity carries thirty-odd columns, a market area and a snapshot row with it.
    /// At national size that was the whole wait - tens of megabytes crossing the wire and being
    /// turned into objects so that all but seven fields of each could be thrown away.
    ///
    /// <para>
    /// A listing with no snapshot arrives here with a <see cref="Price"/> and a
    /// <see cref="PricePerM2"/> of zero rather than being dropped in SQL, because deciding what
    /// counts as measurable is the calculator's rule and belongs in one place - see
    /// MarketOverviewCalculator.CollectMeasurable.
    /// </para>
    /// </summary>
    public readonly record struct OverviewListing(
        decimal Price,
        decimal PricePerM2,
        int AreaM2,
        Typology Typology,
        string District,
        string Municipality,
        string Town);
}
