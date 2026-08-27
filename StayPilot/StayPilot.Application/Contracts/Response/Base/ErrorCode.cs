using System.ComponentModel.DataAnnotations;
using System.Reflection;

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
    /// in that order.
    /// </summary>
    public enum ErrorCode
    {
        // ---------- General ----------

        /// <summary>The catch-all for an unhandled exception. {0} is the trace id to search the logs with.</summary>
        [Display(Description = "Something went wrong on our side. Reference: {0}")]
        Unexpected = -1,

        [Display(Description = "The parameter '{0}' is required.")]
        RequiredParameter = -2,

        [Display(Description = "The parameter '{0}' is invalid. It must be {1}.")]
        InvalidParameter = -3,

        [Display(Description = "The parameter '{0}' must be between {1} and {2}.")]
        ParameterOutOfRange = -4,

        /// <summary>Generic "no such row". Prefer the specific code of the area when there is one.</summary>
        [NotFound]
        [Display(Description = "The {0} with id '{1}' was not found.")]
        EntityNotFound = -5,

        /// <summary>The database refused the write. {0} is what it said.</summary>
        [Display(Description = "The changes could not be saved. {0}")]
        SaveFailed = -6,

        [Display(Description = "The list '{0}' has no items.")]
        EmptyList = -7,

        [Display(Description = "A record with the same {0} already exists.")]
        DuplicateValue = -8,

        // ---------- Market areas ----------

        /// <summary>{0} is the address we tried to place, as sent by the caller.</summary>
        [NotFound]
        [Display(Description = "No market area matches the address '{0}'.")]
        MarketAreaNotFound = -100,

        /// <summary>The caller passed a MarketAreaId that is not in the table.</summary>
        [NotFound]
        [Display(Description = "The market area with id '{0}' was not found.")]
        MarketAreaIdNotFound = -101,

        /// <summary>
        /// A stats recalculation found nothing it could use. {0} is how many listings were read:
        /// zero means no listings at all, a number means none of them had a price and a place.
        /// </summary>
        [Display(Description = "Not enough listings to work out market area stats. Listings read: {0}.")]
        NotEnoughListingsForStats = -102,

        // ---------- Property listings ----------

        [NotFound]
        [Display(Description = "The property listing with id '{0}' was not found.")]
        PropertyListingNotFound = -200,

        /// <summary>{0} is the source url, so a bulk caller knows which of its listings this is.</summary>
        [Display(Description = "The listing '{0}' needs both a latitude and a longitude.")]
        ListingLocationRequired = -201,

        /// <summary>One listing inside a bulk request that its batch could not write. {1} is the database message.</summary>
        [Display(Description = "The listing '{0}' could not be saved. {1}")]
        ListingNotSaved = -202,

        /// <summary>
        /// Same as MarketAreaNotFound, but names the listing as well - a bulk caller sends
        /// hundreds at once and needs to know which one we could not place. {1} is the address.
        /// </summary>
        [Display(Description = "The listing '{0}' could not be placed: no market area matches the address '{1}'.")]
        ListingMarketAreaNotFound = -203,

        /// <summary>
        /// Typology 0 is not a room count - it is the gap left when a caller sends nothing, or a
        /// name we do not know. It reaches the front end as a bare number instead of a name and
        /// breaks every screen that reads it as text, so it is refused at the door. {0} is the url.
        /// </summary>
        [Display(Description = "The listing '{0}' needs a typology (T0 to T10).")]
        ListingTypologyRequired = -204,

        /// <summary>
        /// A search whose lowest bound sits above its highest one. It matches nothing, which the
        /// screen used to report as a plain "No listings match" - indistinguishable from a search
        /// that ran correctly and found nothing. {0} names which pair is inverted.
        /// </summary>
        [Display(Description = "The smallest {0} asked for is larger than the largest.")]
        FilterRangeInverted = -205,

        // ---------- Listing snapshots ----------

        [NotFound]
        [Display(Description = "No price snapshot was found for the property with id '{0}'.")]
        SnapshotNotFound = -300,

        [NotFound]
        [Display(Description = "Cannot add a price snapshot: the property with id '{0}' does not exist.")]
        SnapshotPropertyNotFound = -301,

        // ---------- Owned properties ----------

        [NotFound]
        [Display(Description = "The owned property with id '{0}' was not found.")]
        OwnedPropertyNotFound = -400,

        // ---------- Valuation and premium features ----------

        /// <summary>{0} is how many usable listings we have, {1} the minimum the model needs.</summary>
        [Display(Description = "Not enough listings to calculate feature values: found {0}, need at least {1}.")]
        NotEnoughListingsToFitModel = -500,
    }

    /// <summary>
    /// Marks an error that means "the thing you asked for does not exist", so the controller
    /// answers 404 instead of 400. Everything without it is a bad request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotFoundAttribute : Attribute
    {
    }

    /// <summary>
    /// Reads the message and the 404 flag off the enum above.
    /// Both are read once, on first use, and kept in memory - the attributes never change.
    /// </summary>
    public static class ErrorCodes
    {
        private static readonly Dictionary<ErrorCode, string> Messages = ReadMessages();
        private static readonly HashSet<ErrorCode> NotFoundCodes = ReadNotFoundCodes();

        /// <summary>
        /// The message for this code, with {0}, {1}... replaced by the values passed in.
        /// </summary>
        public static string Format(this ErrorCode code, params string[] messageParameters)
        {
            var message = Messages.TryGetValue(code, out var found) ? found : code.ToString();

            return messageParameters.Length == 0 ? message : string.Format(message, messageParameters);
        }

        /// <summary>True when the code is marked [NotFound].</summary>
        public static bool IsNotFound(this ErrorCode code) => NotFoundCodes.Contains(code);

        /// <summary>True when the error was built from a code marked [NotFound].</summary>
        public static bool IsNotFound(this Error error) => ((ErrorCode)error.ErrorCode).IsNotFound();

        private static Dictionary<ErrorCode, string> ReadMessages()
        {
            var messages = new Dictionary<ErrorCode, string>();

            foreach (var field in typeof(ErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var code = (ErrorCode)field.GetValue(null)!;

                // No [Display] on a code is a mistake, but showing its name beats showing nothing.
                messages[code] = field.GetCustomAttribute<DisplayAttribute>()?.Description ?? field.Name;
            }

            return messages;
        }

        private static HashSet<ErrorCode> ReadNotFoundCodes()
        {
            var codes = new HashSet<ErrorCode>();

            foreach (var field in typeof(ErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetCustomAttribute<NotFoundAttribute>() is not null)
                {
                    codes.Add((ErrorCode)field.GetValue(null)!);
                }
            }

            return codes;
        }
    }
}
