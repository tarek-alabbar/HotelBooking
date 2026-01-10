using Microsoft.AspNetCore.Mvc;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Api.Application.Services;

var builder = WebApplication.CreateBuilder(args);

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
});


builder.Services.AddHealthChecks();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
        options.RoutePrefix = string.Empty; // Swagger at '/'
    });
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
