using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HallApp.Application.Services;
using HallApp.Core.Interfaces.IServices;

namespace HallApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection));
        
        // MediatR
        // services.AddMediatR(typeof(DependencyInjection));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IHallService, HallService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<INotificationService, NotificationService>();
        
        // Register Customer services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        
        // Register Vendor services
        services.AddScoped<IVendorManagerService, VendorManagerService>();
        services.AddScoped<IVendorProfileService, VendorProfileService>();
        
        // Register HallManager services
        services.AddScoped<IHallManagerService, HallManagerService>();
        services.AddScoped<IHallManagerProfileService, HallManagerProfileService>();
        
        return services;
    }
}
