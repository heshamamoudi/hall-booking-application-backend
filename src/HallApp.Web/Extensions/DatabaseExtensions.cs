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
                
                // Check database connection
                bool canConnect = await context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    logger?.LogWarning("Cannot connect to database - skipping database operations");
                    return;
                }
                
                logger?.LogInformation("Connected to database successfully");
                
                // Apply migrations
                await context.Database.MigrateAsync();
                logger?.LogInformation("Database migrations applied");
                
                // Seed data
                await SeedAll.SeedAllData(userManager, roleManager, context);
                logger?.LogInformation("Database seeding completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Database setup failed - continuing without database");
            }
        }
    }
}
