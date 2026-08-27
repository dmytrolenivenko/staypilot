using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Contracts.Response.Base;
using StayPilot.Infrastructure.Persistence;
using StayPilot.Application.Services;
using StayPilot.Infrastructure.Repositories;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;
using Microsoft.Identity.Web;

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

// Register the repositories (the classes that read and write the database).
builder.Services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
builder.Services.AddScoped<IMarketAreaRepository, MarketAreaRepository>();
builder.Services.AddScoped<IBeachMarkerRepository, BeachMarkerRepository>();
builder.Services.AddScoped<IListingSnapshotRepository, ListingSnapshotRepository>();
builder.Services.AddScoped<IOwnedPropertyRepository, OwnedPropertyRepository>();
builder.Services.AddScoped<IPremiumFeatureRepository, PremiumFeatureRepository>();
builder.Services.AddScoped<IMarketAreaStatsRepository, MarketAreaStatsRepository>();
builder.Services.AddScoped<IHousePriceGrowthRepository, HousePriceGrowthRepository>();

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

// Swagger JSON and the test page in the browser.
app.UseSwagger();
app.UseSwaggerUI();

// Send HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Check the user is authenticated (logged in).
app.UseAuthentication();

// Check the user is allowed to call the endpoint.
app.UseAuthorization();

// Send each request to the matching controller.
app.MapControllers();

// Start the app.
app.Run();
