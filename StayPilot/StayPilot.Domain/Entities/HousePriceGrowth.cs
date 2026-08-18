namespace StayPilot.Domain.Entities
{
    /// <summary>
    /// The long run house price growth assumed for one Portuguese district, in percent per year.
    ///
    /// Reference data, seeded from a migration and never written by the API. It exists because
    /// this database only holds a few months of adverts in the Algarve: a ten year projection
    /// built purely on that trend would extrapolate one season of one region across the country.
    /// The seeded figure carries the long run; the local trend measured from snapshots pulls it
    /// toward what is actually happening around the property. Both are shown separately, so a
    /// forecast never hides which half it came from.
    ///
    /// The national row (District = "") is the fallback for any district with no row of its own.
    /// </summary>
    public class HousePriceGrowth
    {
        /// <summary>
        /// Database Id for this row.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// District the figure applies to. Empty string is the national fallback row.
        /// </summary>
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// Assumed house price growth for this district, percent per year.
        /// </summary>
        public decimal AnnualGrowthPercent { get; set; }

        /// <summary>
        /// How much a single year can plausibly differ from the figure above, in percentage
        /// points. Drives the conservative and optimistic scenarios rather than a fixed guess,
        /// because a district that swings hard deserves a wider fan than one that does not.
        /// </summary>
        public decimal VolatilityPercentagePoints { get; set; }

        /// <summary>
        /// Where the figure came from, in words. Printed on screen next to the forecast so the
        /// number is never read as a measurement of this database.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The year this assumption was last reviewed.
        /// </summary>
        public int AsOfYear { get; set; }
    }
}
