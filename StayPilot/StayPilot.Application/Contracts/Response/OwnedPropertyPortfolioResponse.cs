using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Application.Contracts.Response.SubResponse;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Every owned property priced in one go, with what its place is doing and where its asking
    /// price is heading. The Valuation screen reads this and nothing else to draw its list.
    ///
    /// Every price on this response is an ASKING price - what the adverts around the property
    /// want, not what anyone paid. See <see cref="AskSpreadSummary"/> for why that distinction
    /// is kept in the field names rather than left to a tooltip.
    ///
    /// One request rather than one per property because the valuation model is fitted over the
    /// whole listing table: fitting it once and pricing ten properties against it is the same work
    /// as pricing one, while ten separate calls is ten fits.
    /// </summary>
    public class OwnedPropertyPortfolioResponse : ResponseBase
    {
        /// <summary>The properties, dearest first. Empty when the user has none.</summary>
        public List<OwnedPropertyPortfolioItemResponse> Items { get; set; } = new();

        /// <summary>How many properties were priced.</summary>
        public int PropertyCount { get; set; }

        /// <summary>The estimates added up. Only the ones that could be priced count.</summary>
        public decimal TotalEstimatedAskingPrice { get; set; }

        /// <summary>What was paid for them, added up. Zero for any with no purchase price.</summary>
        public decimal TotalPurchasePrice { get; set; }

        /// <summary>Estimated asking price less what was paid. Not a realised gain.</summary>
        public decimal TotalAskSpreadAmount { get; set; }

        /// <summary>That spread against what was paid, in percent.</summary>
        public decimal TotalAskSpreadPercent { get; set; }

        /// <summary>The Base path total at the end of the projection, across every property.</summary>
        public decimal TotalProjectedAskingPrice { get; set; }

        /// <summary>How many years the projections run for.</summary>
        public int ProjectionYears { get; set; }

        /// <summary>When this was worked out.</summary>
        public DateTime GeneratedAtUtc { get; set; }
    }

    /// <summary>
    /// One owned property: what it is, what it would be advertised at, what its place is doing,
    /// and where that ask is heading. Everything the list row and its expanded panel need except the comps and
    /// the feature breakdown, which stay on the estimate endpoint because they are per property
    /// and large.
    /// </summary>
    public class OwnedPropertyPortfolioItemResponse
    {
        /// <summary>Database Id of the owned property.</summary>
        public int Id { get; set; }

        /// <summary>The name the user gave it.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Apartment, villa, house or land.</summary>
        public PropertyType PropertyType { get; set; }

        /// <summary>Room layout in the Portuguese T-style.</summary>
        public Typology Typology { get; set; }

        /// <summary>Floor area in square metres.</summary>
        public int AreaM2 { get; set; }

        // --- Where it is ------------------------------------------------------------------

        /// <summary>District of the market area it sits in.</summary>
        public string District { get; set; } = string.Empty;

        /// <summary>Município of the market area it sits in.</summary>
        public string Municipality { get; set; } = string.Empty;

        /// <summary>Freguesia of the market area it sits in.</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>The zone the valuation actually priced it as, which can differ from above.</summary>
        public string LocatedAreaName { get; set; } = string.Empty;

        /// <summary>True when the coordinates decided the zone rather than the saved address.</summary>
        public bool LocatedByCoordinates { get; set; }

        // --- What it would be advertised at ------------------------------------------------

        /// <summary>The headline estimate: what it would be advertised at today.</summary>
        public decimal MidPrice { get; set; }

        /// <summary>Low end of the model's own typical error.</summary>
        public decimal MinPrice { get; set; }

        /// <summary>High end of the model's own typical error.</summary>
        public decimal MaxPrice { get; set; }

        /// <summary>The estimated ask divided by the floor area.</summary>
        public decimal PricePerM2 { get; set; }

        /// <summary>How much to trust the estimate.</summary>
        public ValuationConfidence ConfidenceLevel { get; set; }

        /// <summary>
        /// Why the confidence is what it is, when there is something to say. Empty when the
        /// estimate had everything it wanted.
        /// </summary>
        public string ConfidenceNote { get; set; } = string.Empty;

        /// <summary>What was paid, what it would be advertised at now, and the spread between them.</summary>
        public AskSpreadSummary AskSpread { get; set; } = new();

        // --- What its place is doing ------------------------------------------------------

        /// <summary>How keen buyers are around it, with the working.</summary>
        public AreaDemandResponse Demand { get; set; } = new();

        /// <summary>Where its asking price is heading, with both rates behind it kept apart.</summary>
        public GrowthForecastResponse Forecast { get; set; } = new();

        /// <summary>
        /// When this valuation was last calculated. Null when the property has never been
        /// recalculated - it still shows up in the list, just with nothing priced yet.
        /// </summary>
        public DateTime? CalculatedAtUtc { get; set; }
    }
}
