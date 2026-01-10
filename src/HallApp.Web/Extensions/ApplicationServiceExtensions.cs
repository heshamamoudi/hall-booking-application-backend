using HallApp.Infrastructure.Data;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Application.Services;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Data.Repositories;
using HallApp.Web.Services;

namespace HallApp.Web.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        // SignalR is registered in Program.cs - removed duplicate registration
        
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
        services.AddScoped<IChatRepository, ChatRepository>();

        // Register services with validation
        services.AddScoped<ITokenService, TokenService>();
        
        // Register file upload service
        services.AddSingleton<IFileUploadService>(provider =>
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            return new FileUploadService(env.ContentRootPath);
        });
        
        // Validate that all required service interfaces and implementations exist
        try 
        {
            services.AddScoped<IHallService, HallService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVendorManagerService, VendorManagerService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IServiceItemService, ServiceItemService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDashboardExportService, DashboardExportService>();
            services.AddScoped<IChatService, ChatService>();
            // Register BookingFinancialService in the DI container
            services.AddScoped<IBookingFinancialService>(provider =>
                new BookingFinancialService(
                    provider.GetRequiredService<IBookingService>(),
                    provider.GetRequiredService<IServiceItemService>(),
                    provider.GetRequiredService<IVendorService>(),
                    provider.GetRequiredService<ILogger<BookingFinancialService>>()
                ));
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
            typeof(HallApp.Web.Helpers.WebAutoMapperProfiles).Assembly,
            typeof(Program).Assembly
        );

        return services;
    }
}
