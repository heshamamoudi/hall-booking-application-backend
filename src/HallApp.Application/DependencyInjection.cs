using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HallApp.Application.Configuration;
using HallApp.Application.Resilience;
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

        // Invoice Settings Configuration
        services.Configure<InvoiceSettings>(configuration.GetSection(InvoiceSettings.SectionName));

        // Business Rules Configuration (HIGH-008 FIX)
        services.Configure<BusinessRulesSettings>(configuration.GetSection("BusinessRules"));

        // AutoMapper. From v15 the registration takes a configuration action; the
        // Type-marker overload no longer exists. Without a licence key it logs a
        // warning on every start and asks for one in production - the Community
        // tier key is free, see the note in HallApp.Application.csproj.
        var autoMapperLicenseKey = configuration["AutoMapper:LicenseKey"];
        services.AddAutoMapper(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
            {
                cfg.LicenseKey = autoMapperLicenseKey;
            }

            cfg.AddMaps(typeof(DependencyInjection).Assembly);
        });
        
        // MediatR
        // services.AddMediatR(typeof(DependencyInjection));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        // Core Application Services (Business Logic - uses IUnitOfWork)
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Hall Management Services
        // NOTE: IHallService is also registered in ApplicationServiceExtensions with ISP split interfaces.
        // This registration is kept for assemblies that only use AddApplication().
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

        // HIGH-006: Financial Audit Service
        services.AddScoped<IFinancialAuditService, FinancialAuditService>();

        // HIGH-012: GDPR Data Retention Service
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddHostedService<DataRetentionBackgroundService>();

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

        // HIGH-004 + HIGH-010: HTTP Clients for Payment Providers with retry and circuit breaker policies
        services.AddHttpClient("HyperPay")
            .AddPaymentResiliencePolicies("HyperPay");
        services.AddHttpClient("Tabby")
            .AddPaymentResiliencePolicies("Tabby");
        services.AddHttpClient("Tamara")
            .AddPaymentResiliencePolicies("Tamara");

        return services;
    }
}
