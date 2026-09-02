using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Works out whether one property listing is worth investing in — renovation cost, resale
    /// value, profit — from data this API already holds, plus today's build rates.
    /// </summary>
    public interface IInvestmentAnalysisService
    {
        /// <summary>
        /// Analyzes one listing by its Id. Fails with InvestmentAnalysisNotEnoughData when the
        /// listing's town has no move-in-ready median to resell against.
        /// </summary>
        /// <param name="propertyListingId">The listing to analyze.</param>
        /// <param name="renovationCostOverride">
        /// When set, used instead of the calculated renovation cost — real repair costs vary too
        /// much (self-sourced materials, no labor hired) for one build-rate formula to fit
        /// everyone. Must be zero or greater.
        /// </param>
        /// <param name="cancellationToken"></param>
        Task<InvestmentAnalysisResponse> AnalyzeAsync(int propertyListingId, decimal? renovationCostOverride = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Same analysis as <see cref="AnalyzeAsync"/>, but for one of the user's own properties
        /// instead of a scraped listing — the purchase price stands in for the ask price.
        /// </summary>
        Task<InvestmentAnalysisResponse> AnalyzeOwnedPropertyAsync(int ownedPropertyId, decimal? renovationCostOverride = null, CancellationToken cancellationToken = default);
    }
}
