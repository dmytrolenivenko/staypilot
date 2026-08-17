using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response that carries every owned property of the user.
    /// </summary>
    public class OwnedPropertyListResponse : ResponseBase
    {
        /// <summary>The properties. Empty when the user has none.</summary>
        public List<OwnedPropertyResponse> Items { get; set; } = new();
    }
}
