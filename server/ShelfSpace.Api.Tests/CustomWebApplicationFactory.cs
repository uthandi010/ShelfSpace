using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShelfSpace.Api.Data;

namespace ShelfSpace.Api.Tests;

/// <summary>
/// Boots the real API in-process against a private SQLite in-memory
/// database (one per factory instance) instead of the file-based db the
/// app uses outside of tests. The connection is kept open for the
/// lifetime of the factory, since an in-memory SQLite database is dropped
/// the moment its one connection closes.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The API refuses to start without a signing key of 32+ bytes.
        builder.UseSetting("Jwt:Key", "test-only-jwt-signing-key-0123456789-abcdef");
        builder.UseSetting("Cors:AllowedOrigins:0", "https://allowed.example");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ShelfSpaceContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ShelfSpaceContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
