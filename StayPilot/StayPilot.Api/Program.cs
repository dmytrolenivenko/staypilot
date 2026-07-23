using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StayPilot.Infrastructure.Persistence;
using StayPilot.Application.Services;
using StayPilot.Infrastructure.Repositories;
using StayPilot.Application.Interfaces.Repositories;
using StayPilot.Application.Interfaces.Services;

var builder = WebApplication.CreateBuilder(args);

// Add the controllers. Also tell JSON to write enums as their text name, not a number.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Find all the endpoints so Swagger knows about them.
builder.Services.AddEndpointsApiExplorer();

// Build the Swagger page that shows and tests the API.
builder.Services.AddSwaggerGen();

// Connect to the SQL Server database. The connection string is read from the config.
builder.Services.AddDbContext<StayPilotDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the services (the business logic classes) so they can be injected.
builder.Services.AddScoped<IPropertyListingService, PropertyListingService>();
builder.Services.AddScoped<IMarketAreaService, MarketAreaService>();
builder.Services.AddScoped<IListingSnapshotService, ListingSnapshotService>();
builder.Services.AddScoped<IOwnedPropertyService, OwnedPropertyService>();

// Register the repositories (the classes that read and write the database).
builder.Services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
builder.Services.AddScoped<IMarketAreaRepository, MarketAreaRepository>();
builder.Services.AddScoped<IBeachMarkerRepository, BeachMarkerRepository>();
builder.Services.AddScoped<IListingSnapshotRepository, ListingSnapshotRepository>();
builder.Services.AddScoped<IOwnedPropertyRepository, OwnedPropertyRepository>();


// Turn on ProblemDetails: send errors back in a standard shape.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Catch any unhandled error and turn it into a clean error response.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        // Pick the HTTP status: bad request for a known input error, else 500.
        context.Response.StatusCode = exception switch
        {
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
        await Results.Problem(
            
            title: exception?.Message ?? "An unexpected error occurred.",
            statusCode: context.Response.StatusCode
            ).ExecuteAsync(context);
    });
});

// Set up the HTTP pipeline: the steps every request goes through.
// Only show Swagger when running in development.
if (app.Environment.IsDevelopment())
{
    // Swagger JSON and the test page in the browser.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Send HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Check the user is allowed to call the endpoint.
app.UseAuthorization();

// Send each request to the matching controller.
app.MapControllers();

// Start the app.
app.Run();
