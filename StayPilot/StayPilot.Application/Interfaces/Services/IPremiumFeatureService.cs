using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Handles what each feature (sea view, pool, lift...) is worth market-wide.
    /// </summary>
    public interface IPremiumFeatureService
    {
        /// <summary>
        /// The values from the last recalculation. Empty when it has never run.
        /// </summary>
        Task<PremiumFeatureListResponse> GetAllPremiumFeatures();

        /// <summary>
        /// Measure every feature again from the listings we hold, and store the result.
        /// Comes back carrying NotEnoughListingsToFitModel when there is too little data to
        /// measure anything, and the stored values are left untouched.
        /// </summary>
        Task<PremiumFeatureListResponse> ReCalculatePremiumFeaturesValue();
    }
}
