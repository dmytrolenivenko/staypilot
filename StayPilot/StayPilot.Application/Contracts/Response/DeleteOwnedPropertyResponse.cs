using StayPilot.Application.Contracts.Response.Base;

namespace StayPilot.Application.Contracts.Response
{
    /// <summary>
    /// Response for deleting one owned property.
    /// </summary>
    public class DeleteOwnedPropertyResponse : ResponseBase
    {
        /// <summary>Id of the property that was deleted.</summary>
        public int Id { get; set; }

        /// <summary>Name of the property that was deleted, so the caller can confirm which one it was.</summary>
        public string? Name { get; set; }
    }
}
