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
            // Convert URI format to standard connection string if needed
            var npgsqlConnectionString = ConvertToNpgsqlConnectionString(connectionString);
            Console.WriteLine("🔍 Using PostgreSQL");
            options.UseNpgsql(npgsqlConnectionString);
        }
        else
        {
            Console.WriteLine("🔍 Using SQL Server");
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

    private static string ConvertToNpgsqlConnectionString(string connectionString)
    {
        // If it's already in standard format, return as-is (with SSL added)
        if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            // Add SSL settings if not present
            if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Prefer;Trust Server Certificate=true";
            }
            return connectionString;
        }

        try
        {
            // Parse URI format: postgresql://user:password@host:port/database
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            // Build standard connection string manually (no Npgsql types needed)
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse PostgreSQL URI: {ex.Message}");
            return connectionString;
        }
    }
}
