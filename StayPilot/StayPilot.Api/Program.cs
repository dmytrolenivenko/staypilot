using Anthropic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Infrastructure.Persistence;
using StayPilot.Application.Services;
using StayPilot.Infrastructure.Repositories;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using Microsoft.Identity.Web;
using StayPilot.Api;
using StayPilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add the controllers. Also tell JSON to write enums as their text name, not a number.
// The "Async" suffix is trimmed from action names (the framework default), so the routes
// are /api/OwnedProperty/AddOwnedProperty, not .../AddOwnedPropertyAsync.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Find all the endpoints so Swagger knows about them.
builder.Services.AddEndpointsApiExplorer();

// Build the Swagger page that shows and tests the API.
builder.Services.AddSwaggerGen();

// Add authentication using Azure AD. The appsettings.json file contains the Azure AD settings.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
builder.Services.AddAuthorization();

// Entra External ID (CIAM) quirk: this tenant issues tokens under two different issuer
// hosts depending on how the token was requested - the custom ciamlogin.com domain
// (Postman's raw Authorization Code POST) or the canonical login.microsoftonline.com
// host (MSAL.js, used by the Angular app). Same tenant either way, so accepting both is
// not a validation weakening - PostConfigure so this runs after Microsoft.Identity.Web's
// own setup, which would otherwise overwrite it back to a single issuer.
var tenantId = builder.Configuration["AzureAd:TenantId"];
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidIssuers = new[]
    {
        $"https://login.microsoftonline.com/{tenantId}/v2.0",
        $"https://staypilot.ciamlogin.com/{tenantId}/v2.0"
    };
});

// Connect to the SQL Server database. The connection string is read from the config.
// Retry on transient errors - Azure SQL is serverless and pauses when idle, so the first
// call after a quiet spell can time out while it wakes up.
builder.Services.AddDbContext<StayPilotDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.EnableRetryOnFailure()));

// Register the services (the business logic classes) so they can be injected.
builder.Services.AddScoped<IPropertyListingService, PropertyListingService>();
builder.Services.AddScoped<IMarketAreaService, MarketAreaService>();
builder.Services.AddScoped<IListingSnapshotService, ListingSnapshotService>();
builder.Services.AddScoped<IOwnedPropertyService, OwnedPropertyService>();
builder.Services.AddScoped<IPremiumFeatureService, PremiumFeatureService>();
builder.Services.AddScoped<IMarketAreaStatsService, MarketAreaStatsService>();
builder.Services.AddScoped<IMarketOverviewService, MarketOverviewService>();
builder.Services.AddScoped<IBuildCostService, BuildCostService>();
builder.Services.AddScoped<IInvestmentAnalysisService, InvestmentAnalysisService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// A singleton, unlike everything above it: it holds the one "is a recalculation going on right
// now" flag that every request has to see, and the run it starts outlives the request that
// asked for it.
builder.Services.AddSingleton<IStatsRecalculationRunner, StatsRecalculationRunner>();

// So CurrentUser can read the logged in user's claims off the current request.
builder.Services.AddHttpContextAccessor();

// One shared client for the whole process, same idea as a shared HttpClient. The key comes
// from Anthropic:ApiKey (User Secrets locally, an Anthropic__ApiKey app setting in prod) rather
// than the SDK's own ANTHROPIC_API_KEY environment variable, so it goes through the same config
// path as everything else in this app. Timeout is cut down from the SDK's 10 minute default -
// this call blocks a single HTTP response, it cannot be allowed to hang that long.
builder.Services.AddSingleton(new AnthropicClient
{
    ApiKey = builder.Configuration["Anthropic:ApiKey"],
    Timeout = TimeSpan.FromSeconds(20)
});
builder.Services.AddScoped<IInvestmentNarrativeClient, ClaudeInvestmentNarrativeClient>();

// Register the repositories (the classes that read and write the database).
builder.Services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
builder.Services.AddScoped<IMarketAreaRepository, MarketAreaRepository>();
builder.Services.AddScoped<IBeachMarkerRepository, BeachMarkerRepository>();
builder.Services.AddScoped<IListingSnapshotRepository, ListingSnapshotRepository>();
builder.Services.AddScoped<IOwnedPropertyRepository, OwnedPropertyRepository>();
builder.Services.AddScoped<IPremiumFeatureRepository, PremiumFeatureRepository>();
builder.Services.AddScoped<IMarketAreaStatsRepository, MarketAreaStatsRepository>();
builder.Services.AddScoped<IHousePriceGrowthRepository, HousePriceGrowthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// The one repository that reads a public statistic instead of the database. Build Cost prices
// itself from INE's construction cost index rather than from a stored price list - no table and
// no migration behind that screen, just an anchor and an index.
//
// INE sends no CORS headers, which is why this proxy exists: the browser cannot read it directly.
builder.Services.AddHttpClient<IIneRepository, IneRepository>(client =>
{
    client.BaseAddress = new Uri("https://www.ine.pt/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

// Turn on ProblemDetails: send errors back in a standard shape.
builder.Services.AddProblemDetails();

// Compress what goes out. Everything this API sends is JSON, which is the most compressible
// thing there is - a leaderboard of every town in the country is three quarters of a megabyte
// raw and well under a tenth of that compressed. Nothing was compressing it before: the app
// runs on Linux App Service with no IIS in front, so if Kestrel does not do it, nobody does.
builder.Services.AddResponseCompression(options =>
{
    // Off by default over HTTPS, which here would mean off entirely. The risk this guards
    // against (BREACH) is about secrets sitting in a compressed response next to attacker-
    // controlled input - these responses carry public market numbers and no session cookie.
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Hold the public market answers for a few minutes.
//
// They only move when a scrape imports and RecalculateMarketAreaStats runs, so working the same
// slice out twice in one minute is pure waste - and on the free App Service plan the CPU that
// wastes is the scarcest thing the app has. Applied per action with [OutputCache], never
// globally: OwnedProperty and the valuation screens are per-user and must never be shared.
builder.Services.AddOutputCache(options =>
{
    // The default is 100MB, which is a lot to hand a free App Service instance with 1GB of RAM
    // and a national listings table to read. Every slice of every place is its own cache entry,
    // so this would fill given enough clicking; 32MB holds far more than a session's worth and
    // evicts the least recently used rather than growing into the memory the queries need.
    options.SizeLimit = 32 * 1024 * 1024;

    options.AddPolicy(OutputCachePolicies.PublicMarketData, new PublicMarketDataCachePolicy());
});

var app = builder.Build();

// The last resort. Everything a caller can actually do something about is already an error on
// the response, so anything that reaches here is a real failure on our side: always a 500, and
// always the same shape as every other error we send, with the trace id to find it in the logs.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var error = new Error(ErrorCode.Unexpected, context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(new { errors = new[] { error } });
    });
});

// Outermost of the two, so what the cache holds is one plain copy of the answer and each
// reply is compressed for whoever asked. The other way round the cache would store whichever
// encoding the first caller happened to accept, and hand it to the next one regardless.
app.UseResponseCompression();

// Swagger JSON and the test page in the browser.
app.UseSwagger();
app.UseSwaggerUI();

// Send HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Check the user is authenticated (logged in).
app.UseAuthentication();

// Check the user is allowed to call the endpoint.
app.UseAuthorization();

// Serve the held copy of a public market answer when there is one. After authorization, so a
// request that is not allowed through never reaches the cache in either direction.
app.UseOutputCache();

// Send each request to the matching controller.
app.MapControllers();

// Start the app.
app.Run();
 