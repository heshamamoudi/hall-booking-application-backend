using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace HallApp.Web.Extensions
{
    public static class DatabaseExtensions
    {
        private static bool _databaseSetupCompleted = false;
        private static readonly object _setupLock = new object();

        public static async Task SetupDatabaseAsync(this IServiceProvider services)
        {
            lock (_setupLock)
            {
                if (_databaseSetupCompleted)
                    return;
                _databaseSetupCompleted = true;
            }

            using var scope = services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var logger = scopedServices.GetService<ILogger<Program>>();

            try
            {
                var context = scopedServices.GetRequiredService<DataContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<AppUser>>();
                var roleManager = scopedServices.GetRequiredService<RoleManager<AppRole>>();

                logger?.LogInformation("Starting database setup...");
                
                // Step 1: Ensure database exists and apply migrations first
                // This will create the database if it doesn't exist
                try
                {
                    logger?.LogInformation("Ensuring database exists and applying migrations...");
                    await context.Database.MigrateAsync();
                    logger?.LogInformation("Database created/updated and migrations applied successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to create database or apply migrations");
                    throw;
                }
                
                // Step 1b: Set all existing users to Active = true
                // (AppUser.Active was previously defaulting to false; fix existing data)
                try
                {
                    var updated = await context.Database.ExecuteSqlRawAsync(
                        @"UPDATE ""Users"" SET ""Active"" = true WHERE ""Active"" = false");
                    if (updated > 0)
                        logger?.LogInformation("Set {Count} existing users to Active = true", updated);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to update existing users Active status");
                }

                // Step 2: Verify database connection after creation/migration
                bool canConnect = false;
                try
                {
                    canConnect = await context.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        logger?.LogInformation("Database connection verified successfully");
                    }
                    else
                    {
                        logger?.LogWarning("Database created but connection verification failed");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Database connection verification failed");
                }
                
                // Seed data
                try
                {
                    var env = scopedServices.GetRequiredService<IHostEnvironment>();
                    var configuration = scopedServices.GetRequiredService<IConfiguration>();
                    var seedLogger = logger ?? scopedServices.GetRequiredService<ILogger<Program>>();
                    await SeedAll.SeedAllData(userManager, roleManager, context, env, configuration, seedLogger);
                    logger?.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Database seeding failed");
                    // Don't throw - seeding failure shouldn't prevent app startup
                }
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "Database setup failed - application cannot start without a working database");
                throw; // Fail fast: migrations must succeed before the app can serve requests
            }
        }
    }
}
