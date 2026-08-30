using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// The result of recalculating one owned property's valuation.
    /// Comes back carrying OwnedPropertyNotFound or NotEnoughListingsToFitModel when it could
    /// not be priced - Item stays null in that case.
    /// </summary>
    public class OwnedPropertyValuationResponse : ResponseBase
    {
        public OwnedPropertyPortfolioItemResponse? Item { get; set; }
    }
}
