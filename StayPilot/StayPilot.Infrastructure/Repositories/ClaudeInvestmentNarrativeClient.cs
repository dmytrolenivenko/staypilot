using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class ClaudeInvestmentNarrativeClient : IInvestmentNarrativeClient
    {
        // A short formatting call over numbers we already trust — the cheapest, fastest model
        // is the right fit, not a reasoning-heavy one.
        private static readonly Model NarrativeModel = Model.ClaudeHaiku4_5_20251001;

        /// <summary>Enough for a few sentences. A thesis that needs more than this is not "short".</summary>
        private const int MaxOutputTokens = 400;

        private readonly AnthropicClient _client;

        public ClaudeInvestmentNarrativeClient(AnthropicClient client)
        {
            _client = client;
        }

        /// <inheritdoc/>
        public async Task<string?> GenerateNarrativeAsync(InvestmentAnalysisResponse result, CancellationToken cancellationToken = default)
        {
            var parameters = new MessageCreateParams
            {
                MaxTokens = MaxOutputTokens,
                Model = NarrativeModel,
                Messages =
                [
                    new()
                    {
                        Role = Role.User,
                        Content = BuildPrompt(result),
                    },
                ],
            };

            try
            {
                var message = await _client.Messages.Create(parameters, cancellationToken);

                return ExtractText(message);
            }
            catch (AnthropicException)
            {
                // Unreachable, rate-limited, or answering with something unexpected. The caller
                // already knows how to return its numbers without a narrative.
                return null;
            }
        }

        /// <summary>Joins every text block Claude sent back. Null when there was nothing to join.</summary>
        private static string? ExtractText(Message message)
        {
            var text = string.Concat(message.Content
                .Select(block => block.TryPickText(out var textBlock) ? textBlock.Text : string.Empty));

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>
        /// The prompt is the grounding contract: every number Claude may talk about is in the
        /// JSON below it, and it is told so explicitly. Nothing else about the property or the
        /// market goes into the call.
        /// </summary>
        private static string BuildPrompt(InvestmentAnalysisResponse result)
        {
            var grounding = new GroundingData(
                result.AskPrice,
                result.AreaM2,
                result.Condition.ToString(),
                result.District,
                result.Municipality,
                result.Town,
                result.TownMoveInMedianPricePerM2,
                result.TownMoveInListingCount,
                result.EstimatedRenovationCost,
                result.RenovationCostIsOverride,
                result.EstimatedResaleValue,
                result.TotalInvestment,
                result.EstimatedProfit,
                result.ProfitMarginPercent,
                result.Confidence.ToString());

            var json = JsonSerializer.Serialize(grounding);

            return $$"""
                You are writing a short investment thesis for a real estate investor, based ONLY
                on the numbers in the JSON below. Every number in it was already computed by our
                own pricing engine — never compute, estimate, or invent a number of your own, and
                never state a figure that is not present verbatim in this JSON.

                Write 3-5 plain-English sentences covering: whether this looks like a good
                investment, the size of the estimated profit or loss, and how much to trust these
                numbers given Confidence. If Confidence is "Low", say plainly that the numbers
                rest on thin market data. If EstimatedProfit is negative, say plainly that this is
                a loss — do not soften it. If RenovationCostIsOverride is true, EstimatedRenovationCost
                is the investor's own estimate, not a market-rate calculation — refer to it as
                "your renovation estimate" rather than implying our pricing engine derived it.

                {{json}}
                """;
        }

        /// <summary>The only numbers Claude is allowed to talk about — everything already computed, nothing left for it to guess.</summary>
        private sealed record GroundingData(
            decimal AskPrice,
            int AreaM2,
            string Condition,
            string District,
            string Municipality,
            string Town,
            decimal TownMoveInMedianPricePerM2,
            int TownMoveInListingCount,
            decimal EstimatedRenovationCost,
            bool RenovationCostIsOverride,
            decimal EstimatedResaleValue,
            decimal TotalInvestment,
            decimal EstimatedProfit,
            decimal ProfitMarginPercent,
            string Confidence);
    }
}
