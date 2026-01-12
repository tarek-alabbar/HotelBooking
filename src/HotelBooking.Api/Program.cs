using Microsoft.AspNetCore.Mvc;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Api.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Ensure SQLite folder exists (Linux App Service persists under /home)
Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "data"));


// Services
builder.Services.AddDbContext<BookingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' was not found.");

    options.UseSqlite(connectionString);
});

builder.Services.AddControllers();
builder.Services.AddScoped<BookingService>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "A RESTful API for hotel room availability and bookings."
    });

    // Include XML doc comments in Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});


var app = builder.Build();

// Ensure the database exists and is migrated on startup (required for Azure where the DB file may be empty).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.MigrateAsync();
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

var swaggerEnabled = app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    var routePrefix = app.Configuration.GetValue<string>("Swagger:RoutePrefix") ?? "swagger";

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
        options.RoutePrefix = routePrefix;
    });
}


app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Program entry point type marker used by WebApplicationFactory for integration testing.
/// </summary>
public partial class Program { }
