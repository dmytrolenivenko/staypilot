namespace StayPilot.Application.Contracts.Response.SubResponse
{
    /// <summary>
    /// The distance between what was paid for a property and what the model thinks it would be
    /// <em>advertised</em> at today.
    ///
    /// This is deliberately not called a gain, a return or equity. Every number in here is built
    /// from asking prices scraped off portal adverts - what sellers want, never what buyers paid.
    /// Asks in Portugal are negotiated down before a deed is signed, so this spread carries that
    /// gap plus whatever the market has actually done, and the two are not separated. Read it as
    /// "the adverts have moved this far since I bought", not "I have made this much".
    ///
    /// Until <c>PropertyValuation</c> is calibrated against INE's recorded sale prices, no field
    /// on this class may honestly be presented as realised value.
    /// </summary>
    public class AskSpreadSummary
    {
        /// <summary>What you paid. The one figure here that is a real transaction.</summary>
        public decimal PurchasePrice { get; set; }

        /// <summary>
        /// What the model thinks it would be listed at today - the same figure as
        /// <c>MidPrice</c>, copied here so this block stands on its own.
        /// </summary>
        public decimal EstimatedAskingPrice { get; set; }

        /// <summary><see cref="EstimatedAskingPrice"/> less <see cref="PurchasePrice"/>.</summary>
        public decimal SpreadAmount { get; set; }

        /// <summary><see cref="SpreadAmount"/> over <see cref="PurchasePrice"/>, in percent.</summary>
        public decimal SpreadPercent { get; set; }

        /// <summary>Today less the purchase date, in whole years.</summary>
        public int YearsHeld { get; set; }

        /// <summary>
        /// <see cref="SpreadPercent"/> spread over the years held. Null under a year, where
        /// annualising a few weeks of movement magnifies noise into a headline. Null so the
        /// screen can say so - printing 0 there reads as "this property moved nothing", beside
        /// a spread that is really there.
        /// </summary>
        public decimal? SpreadPerYearPercent { get; set; }

        /// <summary>The same per month, which divides sensibly at any age. 0 if not held yet.</summary>
        public decimal SpreadPerMonthPercent { get; set; }
    }
}
