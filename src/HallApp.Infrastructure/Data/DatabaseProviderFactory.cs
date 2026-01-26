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
            Console.WriteLine($"🔍 Using PostgreSQL with connection: {MaskConnectionString(npgsqlConnectionString)}");
            options.UseNpgsql(npgsqlConnectionString);
        }
        else
        {
            Console.WriteLine($"🔍 Using SQL Server with connection: {MaskConnectionString(connectionString)}");
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
        // If it's already in standard format, return as-is
        if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
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

            // Build standard Npgsql connection string
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = Npgsql.SslMode.Prefer, // Railway requires SSL
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse PostgreSQL URI, using as-is: {ex.Message}");
            return connectionString;
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for logging
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(password|pwd)=([^;]*)",
            "$1=****",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
