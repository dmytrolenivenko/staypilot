using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Helpers.Calculators;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Services
{
    /// <inheritdoc/>
    public class InvestmentAnalysisService : IInvestmentAnalysisService
    {
        private readonly IPropertyListingRepository _propertyListingRepo;
        private readonly IOwnedPropertyRepository _ownedPropertyRepo;
        private readonly IMarketAreaRepository _marketAreaRepo;
        private readonly IMarketAreaStatsRepository _marketAreaStatsRepo;
        private readonly IBuildCostService _buildCostService;
        private readonly IInvestmentNarrativeClient _narrativeClient;
        private readonly ICurrentUser _currentUser;

        public InvestmentAnalysisService(
            IPropertyListingRepository propertyListingRepo,
            IOwnedPropertyRepository ownedPropertyRepo,
            IMarketAreaRepository marketAreaRepo,
            IMarketAreaStatsRepository marketAreaStatsRepo,
            IBuildCostService buildCostService,
            IInvestmentNarrativeClient narrativeClient,
            ICurrentUser currentUser
            )
        {
            _propertyListingRepo = propertyListingRepo;
            _ownedPropertyRepo = ownedPropertyRepo;
            _marketAreaRepo = marketAreaRepo;
            _marketAreaStatsRepo = marketAreaStatsRepo;
            _buildCostService = buildCostService;
            _narrativeClient = narrativeClient;
            _currentUser = currentUser;
        }

        /// <inheritdoc/>
        public async Task<InvestmentAnalysisResponse> AnalyzeAsync(int propertyListingId, decimal? renovationCostOverride = null, CancellationToken cancellationToken = default)
        {
            var response = new InvestmentAnalysisResponse();

            if (renovationCostOverride is < 0m)
            {
                response.AddError(ErrorCode.InvalidParameter, nameof(renovationCostOverride), "zero or greater");
                return response;
            }

            var listing = await _propertyListingRepo.GetPropertyListingByIdAsync(propertyListingId);

            if (listing is null)
            {
                response.AddError(ErrorCode.PropertyListingNotFound, propertyListingId.ToString());
                return response;
            }

            var snapshot = listing.ListingSnapshots.OrderByDescending(x => x.SnapshotDateUtc).FirstOrDefault();

            if (snapshot is null)
            {
                response.AddError(ErrorCode.SnapshotNotFound, propertyListingId.ToString());
                return response;
            }

            // Town-level rows for the whole município, with their typology children - a T2 and a
            // T5 do not sell for the same €/m², so resale value is priced off this property's own
            // typology bucket, not the town-wide blend. minListings: 0 because we grade the
            // sample size ourselves via Confidence rather than have the leaderboard gate silently
            // drop a thin town before we get a chance to say so.
            var townStats = await _marketAreaStatsRepo.GetWithTypologiesAsync(
                AreaLevel.Town, minListings: 0, listing.MarketArea.District, listing.MarketArea.Municipality);

            var stats = townStats.FirstOrDefault(x => x.Town == listing.MarketArea.Town);
            var typologyStats = stats?.TypologyStats.FirstOrDefault(x => x.Typology == listing.Typology);

            if (typologyStats is null)
            {
                response.AddError(ErrorCode.InvestmentAnalysisNotEnoughData, listing.MarketArea.Town);
                return response;
            }

            // Same size effect as across typologies, just inside one: a "T5" here can be anything
            // from a modest house to a five-bed villa twice the size. Same +-25% band
            // GetComparablePropertyListingAsync uses for a fair comp.
            const decimal areaBand = 0.25m;
            var isTypicalSizeForTypology = listing.AreaM2 >= typologyStats.MedianAreaM2 * (1 - areaBand)
                && listing.AreaM2 <= typologyStats.MedianAreaM2 * (1 + areaBand);

            if (!isTypicalSizeForTypology)
            {
                response.AddError(ErrorCode.InvestmentAnalysisNotEnoughData, listing.MarketArea.Town);
                return response;
            }

            var buildCostBasis = await _buildCostService.GetBuildCostBasisAsync(cancellationToken);

            var renovationCost = renovationCostOverride
                ?? InvestmentAnalysisCalculator.EstimateRenovationCost(listing.Condition, listing.AreaM2, buildCostBasis);
            var resaleValue = InvestmentAnalysisCalculator.EstimateResaleValue(typologyStats.MedianPricePerM2, listing.AreaM2);
            var totalInvestment = InvestmentAnalysisCalculator.EstimateTotalInvestment(snapshot.Price, renovationCost);
            var profit = InvestmentAnalysisCalculator.EstimateProfit(resaleValue, totalInvestment);
            var profitMarginPercent = InvestmentAnalysisCalculator.EstimateProfitMarginPercent(profit, totalInvestment);
            var confidence = InvestmentAnalysisCalculator.DetermineConfidence(typologyStats.ListingCount);

            response.PropertyListingId = listing.Id;
            response.AskPrice = snapshot.Price;
            response.AreaM2 = listing.AreaM2;
            response.Condition = listing.Condition;
            response.District = listing.MarketArea.District;
            response.Municipality = listing.MarketArea.Municipality;
            response.Town = listing.MarketArea.Town;
            response.TownMoveInMedianPricePerM2 = typologyStats.MedianPricePerM2;
            response.TownMoveInListingCount = typologyStats.ListingCount;
            response.EstimatedRenovationCost = renovationCost;
            response.RenovationCostIsOverride = renovationCostOverride.HasValue;
            response.RenovationOptions = InvestmentAnalysisCalculator.GetRenovationScopeOptions(listing.AreaM2, buildCostBasis);
            response.EstimatedResaleValue = resaleValue;
            response.TotalInvestment = totalInvestment;
            response.EstimatedProfit = profit;
            response.ProfitMarginPercent = profitMarginPercent;
            response.Confidence = confidence;
            response.CalculatedAtUtc = DateTime.UtcNow;

            // Narrated last, from the numbers above and nothing else. Null on any failure — the
            // numbers themselves are the response; the narrative is a bonus on top of them.
            response.Narrative = await _narrativeClient.GenerateNarrativeAsync(response, cancellationToken);

            return response;
        }

        /// <inheritdoc/>
        public async Task<InvestmentAnalysisResponse> AnalyzeOwnedPropertyAsync(int ownedPropertyId, decimal? renovationCostOverride = null, CancellationToken cancellationToken = default)
        {
            var response = new InvestmentAnalysisResponse();

            if (renovationCostOverride is < 0m)
            {
                response.AddError(ErrorCode.InvalidParameter, nameof(renovationCostOverride), "zero or greater");
                return response;
            }

            // Getting HttpCaller Id
            var userId = await _currentUser.GetCurrentUserIdAsync();

            var property = await _ownedPropertyRepo.GetOwnedPropertyAsync(ownedPropertyId, userId);

            if (property is null)
            {
                response.AddError(ErrorCode.OwnedPropertyNotFound, ownedPropertyId.ToString());
                return response;
            }

            // GetOwnedPropertyAsync doesn't eager-load MarketArea (Update and Valuation share it
            // and never needed to), so it's resolved the same way OwnedPropertyService resolves
            // it: a separate lookup by MarketAreaId against the full table.
            var marketAreas = await _marketAreaRepo.GetAllMarketAreasAsync();
            var marketArea = marketAreas.FirstOrDefault(x => x.Id == property.MarketAreaId);

            if (marketArea is null)
            {
                response.AddError(ErrorCode.MarketAreaIdNotFound, property.MarketAreaId.ToString());
                return response;
            }

            var townStats = await _marketAreaStatsRepo.GetWithTypologiesAsync(
                AreaLevel.Town, minListings: 0, marketArea.District, marketArea.Municipality);

            var stats = townStats.FirstOrDefault(x => x.Town == marketArea.Town);
            var typologyStats = stats?.TypologyStats.FirstOrDefault(x => x.Typology == property.Typology);

            if (typologyStats is null)
            {
                response.AddError(ErrorCode.InvestmentAnalysisNotEnoughData, marketArea.Town);
                return response;
            }

            // Same size effect as across typologies, just inside one: a "T5" here can be anything
            // from a modest house to a five-bed villa twice the size. Same +-25% band
            // GetComparablePropertyListingAsync uses for a fair comp.
            const decimal areaBand = 0.25m;
            var isTypicalSizeForTypology = property.AreaM2 >= typologyStats.MedianAreaM2 * (1 - areaBand)
                && property.AreaM2 <= typologyStats.MedianAreaM2 * (1 + areaBand);

            if (!isTypicalSizeForTypology)
            {
                response.AddError(ErrorCode.InvestmentAnalysisNotEnoughData, marketArea.Town);
                return response;
            }

            var buildCostBasis = await _buildCostService.GetBuildCostBasisAsync(cancellationToken);

            var renovationCost = renovationCostOverride
                ?? InvestmentAnalysisCalculator.EstimateRenovationCost(property.Condition, property.AreaM2, buildCostBasis);
            var resaleValue = InvestmentAnalysisCalculator.EstimateResaleValue(typologyStats.MedianPricePerM2, property.AreaM2);
            var totalInvestment = InvestmentAnalysisCalculator.EstimateTotalInvestment(property.PurchasePrice, renovationCost);
            var profit = InvestmentAnalysisCalculator.EstimateProfit(resaleValue, totalInvestment);
            var profitMarginPercent = InvestmentAnalysisCalculator.EstimateProfitMarginPercent(profit, totalInvestment);
            var confidence = InvestmentAnalysisCalculator.DetermineConfidence(typologyStats.ListingCount);

            response.OwnedPropertyId = property.Id;
            response.AskPrice = property.PurchasePrice;
            response.AreaM2 = property.AreaM2;
            response.Condition = property.Condition;
            response.District = marketArea.District;
            response.Municipality = marketArea.Municipality;
            response.Town = marketArea.Town;
            response.TownMoveInMedianPricePerM2 = typologyStats.MedianPricePerM2;
            response.TownMoveInListingCount = typologyStats.ListingCount;
            response.EstimatedRenovationCost = renovationCost;
            response.RenovationCostIsOverride = renovationCostOverride.HasValue;
            response.RenovationOptions = InvestmentAnalysisCalculator.GetRenovationScopeOptions(property.AreaM2, buildCostBasis);
            response.EstimatedResaleValue = resaleValue;
            response.TotalInvestment = totalInvestment;
            response.EstimatedProfit = profit;
            response.ProfitMarginPercent = profitMarginPercent;
            response.Confidence = confidence;
            response.CalculatedAtUtc = DateTime.UtcNow;

            response.Narrative = await _narrativeClient.GenerateNarrativeAsync(response, cancellationToken);

            return response;
        }
    }
}
