using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Services
// --------------------
builder.Services.AddControllers();


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