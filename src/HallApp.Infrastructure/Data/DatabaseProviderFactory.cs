using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data;

public static class DatabaseProviderFactory
{
    public static void ConfigureDatabase(this DbContextOptionsBuilder options, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Detect database provider from connection string
        if (IsPostgreSqlConnectionString(connectionString))
        {
            // PostgreSQL for Railway/cloud deployment
            options.UseNpgsql(connectionString);
        }
        else
        {
            // SQL Server for local development
            options.UseSqlServer(connectionString);
        }
    }

    private static bool IsPostgreSqlConnectionString(string connectionString)
    {
        var lowerConnectionString = connectionString.ToLowerInvariant();
        
        // Check for PostgreSQL indicators
        return lowerConnectionString.Contains("postgresql://") ||
               lowerConnectionString.Contains("postgres://") ||
               lowerConnectionString.Contains("host=") ||
               lowerConnectionString.Contains("user id=postgres") ||
               lowerConnectionString.Contains("username=postgres");
    }
}
