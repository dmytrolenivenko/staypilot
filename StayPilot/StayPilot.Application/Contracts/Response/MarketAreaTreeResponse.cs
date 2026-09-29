using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Contracts.Response.SubResponse;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Every place we hold, as one tree: district, then município, then freguesia, then zone.
    ///
    /// Sent once so the browser can fill the place picker itself. Asking the API for each level
    /// in turn cost a round trip per dropdown - seconds each on a sleepy free-tier host - for
    /// seed data that only changes with a migration.
    /// </summary>
    public class MarketAreaTreeResponse : ResponseBase
    {
        /// <summary>The districts, sorted by name, each carrying everything inside it.</summary>
        public List<MarketAreaTreeDistrictResponse> Districts { get; set; } = new();
    }
}
