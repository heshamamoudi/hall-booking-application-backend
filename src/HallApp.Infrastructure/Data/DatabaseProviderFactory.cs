using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data;

public static class DatabaseProviderFactory
{
    public static void ConfigureDatabase(this DbContextOptionsBuilder options, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "No connection string configured. Set ConnectionStrings__DefaultConnection " +
                "(PostgreSQL: Host=...;Port=5432;Database=...;Username=...;Password=...).",
                nameof(connectionString));
        }

        // Detect database provider from connection string
        if (IsPostgreSqlConnectionString(connectionString))
        {
            // Convert URI format to standard connection string if needed
            var npgsqlConnectionString = ConvertToNpgsqlConnectionString(connectionString);
            Console.WriteLine($"🔍 Using PostgreSQL -> {DescribeTarget(npgsqlConnectionString)}");
            options.UseNpgsql(npgsqlConnectionString);
        }
        else
        {
            Console.WriteLine($"🔍 Using SQL Server -> {DescribeTarget(connectionString)}");
            options.UseSqlServer(connectionString);
        }
    }

    /// <summary>
    /// Renders the server and database a connection string points at, so a deploy aimed
    /// at the wrong target is obvious in the logs. Every other keyword is dropped, which
    /// keeps the password out by construction rather than by remembering to strip it.
    /// </summary>
    internal static string DescribeTarget(string connectionString)
    {
        string? host = null;
        string? database = null;

        foreach (var pair in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0) continue;

            var key = pair[..separator].Trim();
            var value = pair[(separator + 1)..].Trim();

            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                host ??= value;
            }
            else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
            {
                database ??= value;
            }
        }

        return $"host={host ?? "(unknown)"} database={database ?? "(unknown)"}";
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
