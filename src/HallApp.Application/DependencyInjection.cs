using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HallApp.Application.Configuration;
using HallApp.Application.Services;
using HallApp.Application.Services.Payment;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Configuration;

namespace HallApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Payment Settings Configuration
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));

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

        // Invoice Service (must be registered BEFORE BookingService due to dependency)
        services.AddScoped<IInvoiceService, InvoiceService>();

        // Booking Services
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingFinancialService, BookingFinancialService>();

        // Supporting Services
        services.AddScoped<IServiceItemService, ServiceItemService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IAddressService, AddressService>();

        // Payment Provider Services
        services.AddScoped<IPaymentProviderService, HyperPayPaymentService>();
        services.AddScoped<IPaymentProviderService, TabbyPaymentService>();
        services.AddScoped<IPaymentProviderService, TamaraPaymentService>();

        // HTTP Clients for Payment Providers
        services.AddHttpClient("HyperPay");
        services.AddHttpClient("Tabby");
        services.AddHttpClient("Tamara");

        return services;
    }
}
