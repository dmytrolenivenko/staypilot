using System.ComponentModel.DataAnnotations;
using StayPilot.Domain.Enums;

namespace StayPilot.Application.Contracts.Request
{
    /// <summary>
    /// Request for the places at one level, with their price numbers.
    ///
    /// There is no sort or page here on purpose: the whole level is a few hundred rows at most,
    /// so the API hands them all over and the browser sorts and ranks them. Same split the
    /// listing browser uses - filters on the server, ordering in the front.
    /// </summary>
    public class MarketAreaLeaderboardRequest
    {
        /// <summary>
        /// Which grain to return. Municipality by default: districts are too broad to act on and
        /// most towns do not have enough listings to trust.
        /// </summary>
        public AreaLevel Level { get; set; } = AreaLevel.Municipality;

        /// <summary>
        /// Places with fewer listings than this are left out.
        ///
        /// Five by default, not fifteen. Fifteen hid the genuinely cheapest end of the country -
        /// the Alentejo towns where one or two adverts are all there is - so the cheapest place
        /// on the board was not the cheapest place in the data. Five keeps out the single-advert
        /// rows, whose "median" is just that one price, and the front marks anything still thin.
        /// </summary>
        [Range(1, 1000)]
        public int MinListings { get; set; } = 5;
    }
}
