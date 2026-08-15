namespace StayPilot.Application.Contracts.Response.SubResponse
{
    /// <summary>
    /// One line of the "why is it worth this" breakdown: what one feature THIS property has is
    /// worth, priced from the premiums already measured. Only features the property actually has
    /// get a line, and never a negative one - so the lines explain where the value sits, they do
    /// not add up to the headline price.
    /// </summary>
    public class ValuationAdjustment
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        /// <summary>
        /// What the amount was measured against, when the label alone would not say - "3 vs 1.8
        /// typical", "2 halvings closer than typical". Null for plain yes/no features, where
        /// having it is the whole story.
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// False when the feature's confidence range straddles zero: the data cannot tell
        /// whether it is worth anything. The amount is still the model's best guess and still
        /// part of the estimate - it is just not a finding, and the screen greys it out rather
        /// than reading a coin flip back as a number.
        /// </summary>
        public bool IsMeasurable { get; set; } = true;
    }
}
