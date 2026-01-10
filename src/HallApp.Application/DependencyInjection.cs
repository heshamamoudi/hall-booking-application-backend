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
        
        // Core Application Services (Business Logic - uses IUnitOfWork)
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Hall Management Services
        services.AddScoped<IHallService, HallService>();
        services.AddScoped<IHallManagerService, HallManagerService>();
        services.AddScoped<IHallManagerProfileService, HallManagerProfileService>();

        // Vendor Management Services
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IVendorManagerService, VendorManagerService>();
        services.AddScoped<IVendorProfileService, VendorProfileService>();
        services.AddScoped<IVendorAvailabilityService, VendorAvailabilityService>();

        // Customer Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();

        // Booking Services
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingFinancialService, BookingFinancialService>();

        // Supporting Services
        services.AddScoped<IServiceItemService, ServiceItemService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAddressService, AddressService>();

        return services;
    }
}
