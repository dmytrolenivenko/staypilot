using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StayPilot.Application.Interfaces;
using StayPilot.Application.Services;
using StayPilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Tells the app to discover all your endpoints so Swagger knows about them.
builder.Services.AddEndpointsApiExplorer();

// Generates the Swagger JSON specification from your controllers.
builder.Services.AddSwaggerGen();

// DbContext registration   
builder.Services.AddDbContext<StayPilotDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI for services
builder.Services.AddScoped<IMarketAreaService, MarketAreaService>();
builder.Services.AddScoped<IPropertyListingService, PropertyListingService>();

// Adding ProblemDetails middleware to handle exceptions and return standardized error responses
builder.Services.AddProblemDetails();

var app = builder.Build();

// Wrapping the app with AddProblemDetails middleware to handle exceptions and return standardized error responses
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // UI of Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
