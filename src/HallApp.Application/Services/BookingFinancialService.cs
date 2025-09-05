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

            // 4. Apply discount (placeholder logic)
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(discountCode))
            {
                // TODO: Implement discount logic based on coupon codes
                _logger.LogInformation("Discount code {DiscountCode} applied", discountCode);
            }

            // 5. Calculate tax
            var taxRate = _taxRates.TryGetValue(region, out var rate) ? rate : _taxRates["DEFAULT"];
            var taxableAmount = subtotal - discountAmount;
            var taxAmount = taxableAmount * taxRate;

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

            _logger.LogInformation("Financial calculation completed. Total: {TotalAmount} SAR", totalAmount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating booking financials");
            throw;
        }
    }
}
