using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HallApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - Auto-detect provider based on connection string
        services.AddDbContext<DataContext>(opt =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            opt.ConfigureDatabase(connectionString);
            
            // Configure global query splitting behavior
            opt.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        });

        return services;
    }
}
