namespace StayPilot.Application.Contracts.Response.Base
{
    /// <summary>
    /// Every error the API can return, with its message right next to it.
    ///
    /// Numbers are grouped by area, so a code alone tells you where it came from:
    ///   -1   to  -99   general, any service can use these
    ///   -100 to -199   market areas
    ///   -200 to -299   property listings
    ///   -300 to -399   listing snapshots
    ///   -400 to -499   owned properties
    ///   -500 to -599   valuation and premium features
    ///
    /// Two rules: add new codes at the end of their band, and never reuse a number.
    /// Callers key on the number, so reusing one silently changes what their code means.
    ///
    /// The {0}, {1}... in a message are filled in from the values you pass to AddError,
    /// in that order. The message and the "does this mean 404" flag for each code live in
    /// <see cref="ErrorCodes"/>, right below.
    /// </summary>
    public enum ErrorCode
    {
        // ---------- General ----------

        /// <summary>The catch-all for an unhandled exception. {0} is the trace id to search the logs with.</summary>
        Unexpected = -1,

        InvalidParameter = -3,

        // ---------- Market areas ----------

        /// <summary>{0} is the address we tried to place, as sent by the caller.</summary>
        MarketAreaNotFound = -100,

        /// <summary>The caller passed a MarketAreaId that is not in the table.</summary>
        MarketAreaIdNotFound = -101,

        /// <summary>
        /// A stats recalculation found nothing it could use. {0} is how many listings were read:
        /// zero means no listings at all, a number means none of them had a price and a place.
        /// </summary>
        NotEnoughListingsForStats = -102,

        // ---------- Property listings ----------

        PropertyListingNotFound = -200,

        /// <summary>{0} is the source url, so a bulk caller knows which of its listings this is.</summary>
        ListingLocationRequired = -201,

        /// <summary>One listing inside a bulk request that its batch could not write. {1} is the database message.</summary>
        ListingNotSaved = -202,

        /// <summary>
        /// Same as MarketAreaNotFound, but names the listing as well - a bulk caller sends
        /// hundreds at once and needs to know which one we could not place. {1} is the address.
        /// </summary>
        ListingMarketAreaNotFound = -203,

        /// <summary>
        /// Typology 0 is not a room count - it is the gap left when a caller sends nothing, or a
        /// name we do not know. It reaches the front end as a bare number instead of a name and
        /// breaks every screen that reads it as text, so it is refused at the door. {0} is the url.
        /// </summary>
        ListingTypologyRequired = -204,

        /// <summary>
        /// A search whose lowest bound sits above its highest one. It matches nothing, which the
        /// screen used to report as a plain "No listings match" - indistinguishable from a search
        /// that ran correctly and found nothing. {0} names which pair is inverted.
        /// </summary>
        FilterRangeInverted = -205,

        // ---------- Listing snapshots ----------

        SnapshotNotFound = -300,

        SnapshotPropertyNotFound = -301,

        // ---------- Owned properties ----------

        OwnedPropertyNotFound = -400,

        // ---------- Valuation and premium features ----------

        /// <summary>{0} is how many usable listings we have, {1} the minimum the model needs.</summary>
        NotEnoughListingsToFitModel = -500,
    }

    /// <summary>
    /// The message and the "does this mean 404" flag for each <see cref="ErrorCode"/>.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// The message for this code, with {0}, {1}... replaced by the values passed in.
        /// </summary>
        public static string Format(this ErrorCode code, params string[] messageParameters)
        {
            var message = MessageFor(code);

            return messageParameters.Length == 0 ? message : string.Format(message, messageParameters);
        }

        /// <summary>True when this code means "the thing you asked for does not exist" (404). Everything else is a 400.</summary>
        public static bool IsNotFound(this ErrorCode code) => code switch
        {
            ErrorCode.MarketAreaNotFound => true,
            ErrorCode.MarketAreaIdNotFound => true,
            ErrorCode.PropertyListingNotFound => true,
            ErrorCode.SnapshotNotFound => true,
            ErrorCode.SnapshotPropertyNotFound => true,
            ErrorCode.OwnedPropertyNotFound => true,
            _ => false,
        };

        /// <summary>True when the error was built from a code that means "not found".</summary>
        public static bool IsNotFound(this Error error) => ((ErrorCode)error.ErrorCode).IsNotFound();

        private static string MessageFor(ErrorCode code) => code switch
        {
            ErrorCode.Unexpected => "Something went wrong on our side. Reference: {0}",
            ErrorCode.InvalidParameter => "The parameter '{0}' is invalid. It must be {1}.",

            ErrorCode.MarketAreaNotFound => "No market area matches the address '{0}'.",
            ErrorCode.MarketAreaIdNotFound => "The market area with id '{0}' was not found.",
            ErrorCode.NotEnoughListingsForStats => "Not enough listings to work out market area stats. Listings read: {0}.",

            ErrorCode.PropertyListingNotFound => "The property listing with id '{0}' was not found.",
            ErrorCode.ListingLocationRequired => "The listing '{0}' needs both a latitude and a longitude.",
            ErrorCode.ListingNotSaved => "The listing '{0}' could not be saved. {1}",
            ErrorCode.ListingMarketAreaNotFound => "The listing '{0}' could not be placed: no market area matches the address '{1}'.",
            ErrorCode.ListingTypologyRequired => "The listing '{0}' needs a typology (T0 to T10).",
            ErrorCode.FilterRangeInverted => "The smallest {0} asked for is larger than the largest.",

            ErrorCode.SnapshotNotFound => "No price snapshot was found for the property with id '{0}'.",
            ErrorCode.SnapshotPropertyNotFound => "Cannot add a price snapshot: the property with id '{0}' does not exist.",

            ErrorCode.OwnedPropertyNotFound => "The owned property with id '{0}' was not found.",

            ErrorCode.NotEnoughListingsToFitModel => "Not enough listings to calculate feature values: found {0}, need at least {1}.",

            _ => code.ToString(),
        };
    }
}
