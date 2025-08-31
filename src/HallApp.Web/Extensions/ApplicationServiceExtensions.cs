using HallApp.Infrastructure.Data;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Application.Services;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data.Repositories;

namespace HallApp.Web.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        // Add SignalR service registration
        services.AddSignalR();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register repositories
        services.AddScoped<IHallRepository, HallRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVendorBookingRepository, VendorBookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceItemRepository, ServiceItemRepository>();
        services.AddScoped<IVendorAvailabilityRepository, VendorAvailabilityRepository>();
        services.AddScoped<IVendorManagerRepository, VendorManagerRepository>();

        // Register services with validation
        services.AddScoped<ITokenService, TokenService>();
        
        // Validate that all required service interfaces and implementations exist
        try 
        {
            services.AddScoped<IHallService, HallService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVendorManagerService, VendorManagerService>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to register application services. Check that all service interfaces and implementations are available.", ex);
        }

        // Add security headers service
        services.AddAntiforgery(options => {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
        });

        // AutoMapper configuration - ensure we scan all assemblies with profiles
        services.AddAutoMapper(
            typeof(HallApp.Application.Helpers.AutoMapperProfiles).Assembly,
            typeof(Program).Assembly
        );

        return services;
    }
}
