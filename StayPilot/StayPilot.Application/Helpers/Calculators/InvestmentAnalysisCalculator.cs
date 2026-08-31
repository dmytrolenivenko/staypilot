
using StayPilot.Application.Contracts.Response;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Helpers.Calculators
{
    public static class InvestmentAnalysisCalculator
    {
        /// <summary>
        /// Which build-cost tier a renovation would be costed at for each condition, or null
        /// when the property is already move-in ready and needs no renovation.
        /// </summary>
        private static string? RenovationTierFor(PropertyCondition condition)
        {
            switch (condition)
            {
                case PropertyCondition.NeedsRenovation:
                    return "standard";
                case PropertyCondition.Used:
                    return "economy";
                case PropertyCondition.Unknown:
                    return "economy";
                default:
                    return null;
            }
        }

        /// <summary>
        /// What it would cost to bring this property to move-in condition, at today's build
        /// rates. Zero when the property is already move-in ready.
        /// </summary>
        public static decimal EstimateRenovationCost(PropertyCondition condition, int areaM2, BuildCostBasisResponse buildCostBasis)
        {
            var tierKey = RenovationTierFor(condition);

            if (tierKey is null)
            {
                return 0m;
            }

            var tier = buildCostBasis.Tiers.FirstOrDefault(t => t.Key == tierKey);

            return (tier?.RatePerM2 ?? 0m) * areaM2;
        }

        /// <summary>
        /// What this property would sell for once brought to move-in condition, at this
        /// property's own typology median €/m² in its town — a T2 and a T5 do not sell for the
        /// same €/m², so a blended, town-wide figure would misprice either one. The caller is
        /// expected to have already checked that median exists — see
        /// <see cref="Services.InvestmentAnalysisService"/>'s "not enough data" gate — so this
        /// never has to decide what a missing median means.
        /// </summary>
        public static decimal EstimateResaleValue(decimal typologyMedianPricePerM2, int areaM2)
        {
            return typologyMedianPricePerM2 * areaM2;
        }

        /// <summary>Ask price plus whatever renovation it takes to reach move-in condition.</summary>
        public static decimal EstimateTotalInvestment(decimal askPrice, decimal renovationCost)
        {
            return askPrice + renovationCost;
        }

        /// <summary>Resale value minus everything put into the property. Negative is a loss.</summary>
        public static decimal EstimateProfit(decimal resaleValue, decimal totalInvestment)
        {
            return resaleValue - totalInvestment;
        }

        /// <summary>Profit as a percent of what was put in. Zero on the degenerate case of nothing put in.</summary>
        public static decimal EstimateProfitMarginPercent(decimal profit, decimal totalInvestment)
        {
            if (totalInvestment <= 0m)
            {
                return 0m;
            }

            return decimal.Round(profit / totalInvestment * 100m, 1);
        }

        /// <summary>
        /// Renovation scope options, as ratios of the INE-derived "standard" build rate. Only
        /// "Full rebuild" (1.00) is INE's own number — INE publishes a cost for building from
        /// scratch, not for renovating, so "Cosmetic" and "Full renovation" are StayPilot
        /// estimates of what a lighter scope of work costs relative to that. Lets an investor
        /// compare scopes up front instead of typing a guess into renovationCostOverride blind.
        /// </summary>
        private static readonly (string Key, string Label, decimal Ratio, string Note)[] RenovationScopes =
        [
            ("cosmetic", "Cosmetic", 0.25m, "Paint, fixtures, no structural work — StayPilot estimate"),
            ("renovation", "Full renovation", 0.60m, "New systems, finishes, layout — structure kept — StayPilot estimate"),
            ("rebuild", "Full rebuild", 1.00m, "Demolish & construct new — INE's standard build rate")
        ];

        /// <summary>
        /// The fixed-price renovation options for the investor to pick from before committing to
        /// one, priced off the INE-derived "standard" build tier. Zero rate when that tier is
        /// missing from <paramref name="buildCostBasis"/> — should not happen, but a €0 option
        /// beats a crash.
        /// </summary>
        public static List<BuildCostOption> GetRenovationScopeOptions(int areaM2, BuildCostBasisResponse buildCostBasis)
        {
            var standardRate = buildCostBasis.Tiers.FirstOrDefault(t => t.Key == "standard")?.RatePerM2 ?? 0m;

            return RenovationScopes
                .Select(scope => new BuildCostOption
                {
                    Key = scope.Key,
                    Label = scope.Label,
                    RatePerM2 = decimal.Round(standardRate * scope.Ratio, 0, MidpointRounding.AwayFromZero),
                    Cost = decimal.Round(standardRate * scope.Ratio * areaM2, 0, MidpointRounding.AwayFromZero),
                    Note = scope.Note
                })
                .ToList();
        }

        /// <summary>Below this many comps of the property's typology, the median is real but still thin.</summary>
        private const int MediumConfidenceMoveInCount = 8;

        /// <summary>At or above this many comps of the property's typology, the median is well supported.</summary>
        private const int HighConfidenceMoveInCount = 15;

        /// <summary>
        /// How much to trust <see cref="EstimateResaleValue"/>, judged on how many comps of the
        /// property's own typology the median actually rests on. The caller only reaches here
        /// once that count has already cleared MarketAreaStatsCalculator's own minimum of 3 for a
        /// typology median to exist at all — this just grades how far past that floor it is.
        /// </summary>
        public static ValuationConfidence DetermineConfidence(int typologyListingCount)
        {
            if (typologyListingCount >= HighConfidenceMoveInCount)
            {
                return ValuationConfidence.High;
            }

            if (typologyListingCount >= MediumConfidenceMoveInCount)
            {
                return ValuationConfidence.Medium;
            }

            return ValuationConfidence.Low;
        }
    }
}
