using HallApp.Core.Entities;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
                    await SeedAll.SeedAllData(userManager, roleManager, context);
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
                logger?.LogError(ex, "Database setup failed - continuing without database operations");
                // Don't rethrow to prevent app startup failure
            }
        }
    }
}
