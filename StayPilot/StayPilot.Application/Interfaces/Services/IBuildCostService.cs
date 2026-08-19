using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Prices the Build Cost screen from INE's live construction cost index.
    /// </summary>
    public interface IBuildCostService
    {
        /// <summary>
        /// Every rate the screen needs, escalated to the latest month INE has published.
        /// Always returns a populated response: when INE cannot be reached the rates come back at
        /// 2021 prices with <c>Index</c> null, which is more use than an error page.
        /// </summary>
        Task<BuildCostBasisResponse> GetBuildCostBasisAsync(CancellationToken cancellationToken = default);
    }
}
