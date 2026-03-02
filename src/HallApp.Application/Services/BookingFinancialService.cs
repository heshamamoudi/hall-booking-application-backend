using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

public class BookingFinancialService : IBookingFinancialService
{
    private readonly IBookingService _bookingService;
    private readonly IServiceItemService _serviceItemService;
    private readonly IVendorService _vendorService;
    private readonly ILogger<BookingFinancialService> _logger;

    // Tax rates by region (Saudi Arabia VAT = 15%)
    // CRIT-FIN-002: Use decimal fraction format (0.15 = 15%)
    private readonly Dictionary<string, decimal> _taxRates = new()
    {
        { "Riyadh", 0.15m },
        { "Jeddah", 0.15m },
        { "Dammam", 0.15m },
        { "DEFAULT", 0.15m }
    };

    public BookingFinancialService(
        IBookingService bookingService,
        IServiceItemService serviceItemService,
        IVendorService vendorService,
        ILogger<BookingFinancialService> logger)
    {
        _bookingService = bookingService;
        _serviceItemService = serviceItemService;
        _vendorService = vendorService;
        _logger = logger;
    }

    public async Task<BookingFinancialResult> CalculateBookingFinancialsAsync(
        int hallId,
        DateTime eventStart,
        DateTime eventEnd,
        List<BookingServiceRequest> services,
        string discountCode = "",
        string region = "Riyadh")
    {
        _logger.LogInformation("Starting financial calculation for booking HallId: {HallId}", hallId);

        try
        {
            // 1. Calculate hall cost
            var hallCost = await _bookingService.CalculateBookingCostAsync(hallId, eventStart, eventEnd);

            // 2. Calculate vendor services cost
            var vendorBreakdown = new List<VendorFinancialResult>();
            decimal vendorServicesCost = 0;

            if (services?.Any() == true)
            {
                var servicesByVendor = services.GroupBy(s => s.VendorId);

                foreach (var vendorGroup in servicesByVendor)
                {
                    var vendorId = vendorGroup.Key;
                    var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                    var vendorName = vendor?.Name ?? $"Vendor {vendorId}";

                    var vendorServices = new List<ServiceFinancialResult>();
                    decimal vendorTotal = 0;

                    foreach (var service in vendorGroup)
                    {
                        var serviceItem = await _serviceItemService.GetServiceItemByIdAsync(service.ServiceItemId);
                        var unitPrice = serviceItem?.Price ?? 0;
                        var totalPrice = unitPrice * service.Quantity;

                        vendorServices.Add(new ServiceFinancialResult
                        {
                            ServiceItemId = service.ServiceItemId,
                            ServiceName = serviceItem?.Name ?? $"Service {service.ServiceItemId}",
                            UnitPrice = unitPrice,
                            Quantity = service.Quantity,
                            TotalPrice = totalPrice
                        });

                        vendorTotal += totalPrice;
                    }

                    vendorBreakdown.Add(new VendorFinancialResult
                    {
                        VendorId = vendorId,
                        VendorName = vendorName,
                        TotalAmount = vendorTotal,
                        Services = vendorServices
                    });

                    vendorServicesCost += vendorTotal;
                }
            }

            // 3. Calculate subtotal
            var subtotal = hallCost + vendorServicesCost;

            // 4. Apply discount (CRIT-FIN-001)
            decimal discountAmount = CalculateDiscount(discountCode, subtotal);

            // 5. Calculate tax (CRIT-FIN-002: uses decimal fraction rate, e.g. 0.15 = 15%)
            var taxRate = _taxRates.TryGetValue(region, out var rate) ? rate : _taxRates["DEFAULT"];
            var taxableAmount = subtotal - discountAmount;
            var taxAmount = Math.Round(taxableAmount * taxRate, 2, MidpointRounding.AwayFromZero);

            // 6. Calculate total
            var totalAmount = taxableAmount + taxAmount;

            var result = new BookingFinancialResult
            {
                HallCost = hallCost,
                VendorServicesCost = vendorServicesCost,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                TaxRate = taxRate,
                TotalAmount = totalAmount,
                Currency = "SAR",
                VendorBreakdown = vendorBreakdown
            };

            _logger.LogInformation(
                "Financial calculation completed. HallCost: {HallCost}, VendorCost: {VendorCost}, " +
                "Discount: {Discount}, Tax: {Tax}, Total: {TotalAmount} SAR",
                hallCost, vendorServicesCost, discountAmount, taxAmount, totalAmount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating booking financials");
            throw;
        }
    }

    /// <summary>
    /// CRIT-FIN-001: Calculate discount amount from coupon code.
    /// Currently the coupon system (CouponRepository, coupon validation, discount types)
    /// is not yet implemented in the database layer. This method returns 0 and logs a warning
    /// when a discount code is provided.
    ///
    /// TODO: When CouponRepository is implemented, replace this with:
    ///   1. Look up the coupon by code via ICouponRepository.GetActiveCouponByCodeAsync(code)
    ///   2. Validate: IsActive, not expired, minimum order amount, usage limits
    ///   3. Calculate: Percentage (subtotal * rate / 100) or FixedAmount
    ///   4. Apply MaxDiscountAmount cap if configured
    ///   5. Return the computed discount amount
    /// </summary>
    private decimal CalculateDiscount(string discountCode, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
        {
            return 0;
        }

        // TODO: Implement coupon validation when CouponRepository is available.
        // The Coupon entity exists but has no discount fields, no DbSet, and no repository yet.
        _logger.LogWarning(
            "CRIT-FIN-001: Discount code '{DiscountCode}' was provided but the coupon system is not yet " +
            "implemented. Discount amount returned as 0. Subtotal: {Subtotal} SAR",
            discountCode, subtotal);

        return 0;
    }
}
