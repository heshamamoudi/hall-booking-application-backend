using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HallApp.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        // Get environment name
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Dynamically find the content root
        var basePath = Directory.GetCurrentDirectory();

        Console.WriteLine($"[DbContextFactory] Base Path: {basePath}");
        Console.WriteLine($"[DbContextFactory] Environment: {environment}");

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string configured. Set ConnectionStrings__DefaultConnection " +
                "(PostgreSQL: Host=...;Port=5432;Database=...;Username=...;Password=...).");
        }

        // Target only - the full string carries the password.
        Console.WriteLine($"[DbContextFactory] Target: {DatabaseProviderFactory.DescribeTarget(connectionString)}");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.ConfigureDatabase(connectionString);

        return new DataContext(optionsBuilder.Options);
    }
}
