namespace StayPilot.Application.Contracts.Response.Base
{
    /// <summary>
    /// One thing that went wrong: the number a caller can key on, and the message a person reads.
    /// Nothing else - the code says which error it is, so it does not need a name beside it.
    /// </summary>
    public class Error
    {
        /// <summary>Needed so the response can be read back from JSON (tests, the scraper's logs).</summary>
        public Error()
        {
        }

        /// <summary>
        /// Builds the error from a code, filling the {0}, {1}... in its message with the
        /// values passed in, in that order.
        /// </summary>
        public Error(ErrorCode code, params string[] messageParameters)
        {
            ErrorCode = (int)code;
            ErrorMessage = code.Format(messageParameters);
        }

        /// <summary>The number from the catalog, for example -2. Always negative.</summary>
        public int ErrorCode { get; set; }

        /// <summary>The finished message, for example "The parameter 'Latitude' is required."</summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
