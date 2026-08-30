using StayPilot.Application.Contracts.Response;

namespace StayPilot.Application.Interfaces.Services
{
    /// <summary>
    /// Turns a fully-computed <see cref="InvestmentAnalysisResponse"/> into a short written
    /// investment thesis. Never computes or invents a number of its own — every figure it may
    /// reference must already be present in the response it is given.
    /// </summary>
    public interface IInvestmentNarrativeClient
    {
        /// <summary>
        /// Null on any failure (unreachable, rate-limited, malformed response) — never throws.
        /// The caller is expected to still return its numbers with a null Narrative rather than
        /// fail the whole request over a narration problem.
        /// </summary>
        Task<string?> GenerateNarrativeAsync(InvestmentAnalysisResponse result, CancellationToken cancellationToken = default);
    }
}
