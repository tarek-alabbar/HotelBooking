using System.Data.Common;
using HotelBooking.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.Tests.Fixtures;

/// <summary>
/// Creates an in-memory test host for the API using ASP.NET Core's WebApplicationFactory.
/// This factory swaps the production database configuration for a shared in-memory SQLite database,
/// ensuring fast, deterministic integration tests without external dependencies.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    /// <summary>
    /// Configures the ASP.NET Core test host.
    /// Sets the environment to "Test" and replaces the application's DbContext registration
    /// with an in-memory SQLite provider backed by a shared, open connection.
    /// </summary>
    /// <param name="builder">The web host builder used to configure the test server.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration (e.g., the production Sqlite file-based config)
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BookingDbContext>));

            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // Create and keep open a single in-memory SQLite connection for the test host lifetime.
            // Cache=Shared allows multiple DbContext instances to share the same in-memory database.
            _connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            _connection.Open();

            services.AddDbContext<BookingDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    /// <summary>
    /// Disposes the factory and releases the shared in-memory SQLite connection.
    /// </summary>
    /// <param name="disposing">True when called from Dispose; false when called from finalizer.</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}