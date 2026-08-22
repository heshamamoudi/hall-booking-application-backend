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
        // Resolved here rather than inside the AddDbContext callback: that callback does
        // not run until something first resolves a DataContext, which turns a missing
        // variable into a failure much later and much further from its cause.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No connection string configured. Set ConnectionStrings__DefaultConnection " +
                "(PostgreSQL: Host=...;Port=5432;Database=...;Username=...;Password=...). " +
                "For local development, put it in appsettings.Development.json.");
        }

        // Database Configuration - Auto-detect provider based on connection string
        services.AddDbContext<DataContext>(opt =>
        {
            opt.ConfigureDatabase(connectionString);

            // Configure global query splitting behavior
            opt.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
        });

        // Infrastructure Services (Direct DataContext access for performance-critical operations)
        // These services need direct EF Core access for complex queries and real-time calculations
        services.AddScoped<IHallAvailabilityService, HallAvailabilityService>();
        services.AddScoped<IPriceCalculationService, PriceCalculationService>();

        // Organization & Team Management Services (require DataContext and UserManager)
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();

        return services;
    }
}
