using Microsoft.EntityFrameworkCore;
using StayPilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Tells the app to discover all your endpoints so Swagger knows about them.
builder.Services.AddEndpointsApiExplorer();

// Generates the Swagger JSON specification from your controllers.
builder.Services.AddSwaggerGen();

// DbContext registration   
builder.Services.AddDbContext<StayPilotDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

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
