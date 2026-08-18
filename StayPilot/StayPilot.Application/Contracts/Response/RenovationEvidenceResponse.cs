using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Why the renovation discount for one place should or should not be believed.
    ///
    /// The discount itself is one subtraction between two medians, and two medians always differ
    /// by something. This is the part that says whether that something is a finding: how many
    /// adverts each side rests on, how wide each side's spread is, and how much the two spreads
    /// overlap. A place whose project prices and finished prices cover the same ground has no
    /// measurable discount however far apart its two middle values happen to land.
    /// </summary>
    public class RenovationEvidenceResponse
    {
        /// <summary>
        /// The verdict in one word, for a badge. Everything below it is the working.
        /// </summary>
        public ValuationConfidence Confidence { get; set; }

        /// <summary>
        /// How much of the middle half of the project prices also falls inside the middle half of
        /// the move-in prices, as a percentage of the narrower of the two.
        ///
        /// Zero means the two groups do not overlap at all - project stock is simply cheaper here,
        /// and the discount is real. A hundred means one spread sits entirely inside the other,
        /// and the difference between their medians is noise.
        /// </summary>
        public decimal SpreadOverlapPercent { get; set; }

        /// <summary>
        /// The share of this place's listings that got a verdict at all - project or move-in,
        /// against the total. A discount measured over 12% of the stock is a different claim from
        /// one measured over 80% of it, and the number is the same either way.
        /// </summary>
        public decimal ClassifiedSharePercent { get; set; }

        /// <summary>
        /// The one-line reason behind <see cref="Confidence"/>, naming whichever test it failed:
        /// "only 4 projects", "the two spreads overlap 78%", "resting on 9% of the stock".
        ///
        /// Written on the server so every screen gives the same reason for the same row.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
