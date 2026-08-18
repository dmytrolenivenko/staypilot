using StayPilot.Domain.Entities;

namespace StayPilot.Application.Interfaces.Repositories
{
    /// <summary>
    /// Reads the seeded per-district house price growth assumptions. Read only on purpose:
    /// the rows are reference data that arrives with a migration, so nothing in the API writes
    /// them and a bad forecast can never be blamed on something the app changed at run time.
    /// </summary>
    public interface IHousePriceGrowthRepository
    {
        /// <summary>
        /// The growth assumption for one district, falling back to the national row when that
        /// district has none of its own. Returns null only when even the national row is missing,
        /// which means the seed did not run.
        /// </summary>
        Task<HousePriceGrowth?> GetForDistrictAsync(string district);

        /// <summary>
        /// Every seeded row, national first. Used to show the whole table on screen.
        /// </summary>
        Task<List<HousePriceGrowth>> GetAllAsync();
    }
}
