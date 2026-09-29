using Microsoft.AspNetCore.OutputCaching;

namespace StayPilot.Api
{
    /// <summary>
    /// Holds a public market answer for a few minutes and serves it to everybody who asks the
    /// same question - signed in or not.
    ///
    /// <para>
    /// Written out by hand rather than built with <c>OutputCachePolicyBuilder</c> because the
    /// framework's default policy refuses to cache any request carrying an Authorization header.
    /// That is the right default: most endpoints answer differently per caller, and one caller's
    /// answer handed to the next is a data leak. It is the wrong rule for these endpoints, and
    /// wrong in the case that matters most here - the site signs you in, so every call the app
    /// makes carries a token, and the cache would have sat there serving nobody.
    /// </para>
    ///
    /// <para>
    /// What makes that safe is a property of the endpoints, not of this class: everything under
    /// <see cref="OutputCachePolicies.PublicMarketData"/> answers from the listings table and the
    /// market stats table alone. None of them reads the signed-in user. Putting this policy on an
    /// endpoint that does would serve one person's data to the next caller - see the warning on
    /// <see cref="OutputCachePolicies.PublicMarketData"/>.
    /// </para>
    /// </summary>
    public sealed class PublicMarketDataCachePolicy : IOutputCachePolicy
    {
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var request = context.HttpContext.Request;

            // A GET or a HEAD asks a question; anything else changes something, and the answer to
            // "what did that change" is not a thing to hand to the next caller.
            var cacheable = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);

            context.EnableOutputCaching = cacheable;
            context.AllowCacheLookup = cacheable;
            context.AllowCacheStorage = cacheable;

            // One caller works the answer out and the rest wait for it, rather than fifty
            // identical national reads starting at once on a single shared core.
            context.AllowLocking = true;

            context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(OutputCachePolicies.CacheMinutes);

            // The query string IS the question on every one of these endpoints - the place, the
            // grain, the budget, the sample floor. Cache one answer for all of them and Faro
            // would be served Porto's numbers.
            context.CacheVaryByRules.QueryKeys = "*";

            // Let the browser keep it too, so going back to a screen skips the round trip. Same
            // window as the server copy, and a cached reply carries Age, so it never adds up past it.
            // OnStarting, not ServeResponseAsync: that one runs after the body went out.
            if (cacheable)
            {
                var response = context.HttpContext.Response;

                response.OnStarting(() =>
                {
                    if (response.StatusCode == StatusCodes.Status200OK)
                    {
                        response.Headers.CacheControl = $"public, max-age={OutputCachePolicies.CacheMinutes * 60}";
                    }

                    return Task.CompletedTask;
                });
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var response = context.HttpContext.Response;

            // Only a plain 200 is worth keeping. A failure held for five minutes is a five-minute
            // outage, and a response that sets a cookie is about the caller, not about the market.
            context.AllowCacheStorage =
                response.StatusCode == StatusCodes.Status200OK
                && string.IsNullOrEmpty(response.Headers.SetCookie);

            return ValueTask.CompletedTask;
        }
    }
}
