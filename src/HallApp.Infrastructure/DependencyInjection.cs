using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using HallApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HallApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Configuration - Auto-detect provider based on connection string
        services.AddDbContext<DataContext>(opt =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            opt.ConfigureDatabase(connectionString);

            // Configure global query splitting behavior
            opt.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        });

        // Infrastructure Services (Direct DataContext access for performance-critical operations)
        // These services need direct EF Core access for complex queries and real-time calculations
        services.AddScoped<IHallAvailabilityService, HallAvailabilityService>();
        services.AddScoped<IPriceCalculationService, PriceCalculationService>();

        return services;
    }
}
