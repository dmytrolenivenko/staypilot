using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries the choices for one level of the region picker,
    /// for example every town inside the municipality that was passed in.
    /// </summary>
    public class MarketAreaOptionsResponse : ResponseBase
    {
        /// <summary>The choices, already sorted. Empty when nothing matches the filters.</summary>
        public List<string> Items { get; set; } = new();
    }
}
