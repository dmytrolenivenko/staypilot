using System.Text.Json.Serialization;

namespace StayPilot.Application.Contracts.Response.Base
{
    /// <summary>
    /// What every response can carry: the list of things that went wrong.
    ///
    /// A service records problems here instead of throwing, and the controller turns them into
    /// the HTTP status. The list starts null and is left out of the JSON while it is null, so a
    /// response that worked looks exactly like it did before this class existed.
    /// </summary>
    public abstract class ResponseBase
    {
        /// <summary>Everything that went wrong while handling the request. Null while nothing did.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Error>? Errors { get; set; }

        /// <summary>True while nothing has gone wrong. Not sent to the caller - the HTTP status says it.</summary>
        [JsonIgnore]
        public bool Succeeded => Errors is null || Errors.Count == 0;

        /// <summary>
        /// Records one problem. The values passed in fill the {0}, {1}... in its message.
        /// </summary>
        public void AddError(ErrorCode code, params string[] messageParameters)
        {
            Errors ??= new List<Error>();
            Errors.Add(new Error(code, messageParameters));
        }

        /// <summary>Records a problem that was already built, for example by a validation helper.</summary>
        public void AddError(Error error)
        {
            Errors ??= new List<Error>();
            Errors.Add(error);
        }

        /// <summary>Records problems that were collected somewhere else, for example one item of a bulk request.</summary>
        public void AddErrors(IEnumerable<Error> errors)
        {
            Errors ??= new List<Error>();
            Errors.AddRange(errors);
        }
    }
}
