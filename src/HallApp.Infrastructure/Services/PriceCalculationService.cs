using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

#nullable enable

namespace HallApp.Infrastructure.Services;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly DataContext _context;
    private readonly ILogger<PriceCalculationService> _logger;
    private const decimal VAT_RATE = 0.15m; // 15% VAT for Saudi Arabia

    public PriceCalculationService(DataContext context, ILogger<PriceCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate total booking price including hall cost, vendor services, and taxes
    /// </summary>
    public async Task<BookingPriceBreakdown> CalculateBookingPriceAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime,
        List<int>? serviceItemIds = null,
        string? discountCode = null)
    {
        try
        {
            var breakdown = new BookingPriceBreakdown();

            // 1. Calculate Hall Cost
            var hallCost = await CalculateHallCostAsync(hallId, eventDate, startTime, endTime);
            breakdown.HallCost = hallCost;

            // 2. Calculate Vendor Services Cost
            if (serviceItemIds != null && serviceItemIds.Any())
            {
                var vendorServicesCost = await CalculateVendorServicesAsync(serviceItemIds);
                breakdown.VendorServicesCost = vendorServicesCost;
            }

            // 3. Calculate Subtotal
            breakdown.Subtotal = breakdown.HallCost + breakdown.VendorServicesCost;

            // 4. Apply Discount
            if (!string.IsNullOrEmpty(discountCode))
            {
                var discountAmount = await ApplyDiscountAsync(breakdown.Subtotal, discountCode);
                breakdown.DiscountAmount = discountAmount;
                breakdown.DiscountCode = discountCode;
            }

            // 5. Calculate amount after discount
            var amountAfterDiscount = breakdown.Subtotal - breakdown.DiscountAmount;

            // 6. Calculate VAT (15%)
            breakdown.TaxAmount = await CalculateVatAsync(amountAfterDiscount);
            breakdown.TaxRate = VAT_RATE;

            // 7. Calculate Total
            breakdown.TotalAmount = amountAfterDiscount + breakdown.TaxAmount;
            breakdown.Currency = "SAR";

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating booking price for hall {HallId}", hallId);
            throw;
        }
    }

    /// <summary>
    /// Calculate hall rental cost based on duration and pricing rules
    /// </summary>
    public async Task<decimal> CalculateHallCostAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        try
        {
            var hall = await _context.Halls.FindAsync(hallId);
            if (hall == null)
            {
                throw new ArgumentException($"Hall with ID {hallId} not found");
            }

            // Calculate duration in hours
            var duration = (endTime - startTime).TotalHours;

            // Determine base price based on weekend/weekday (using Both gender pricing as default)
            // Weekend: Friday & Saturday in Saudi Arabia
            var isWeekend = eventDate.DayOfWeek == DayOfWeek.Friday || eventDate.DayOfWeek == DayOfWeek.Saturday;
            var baseHourlyRate = isWeekend ? hall.BothWeekEnds : hall.BothWeekDays;

            // Calculate total cost
            decimal totalCost = (decimal)(baseHourlyRate * duration);

            _logger.LogInformation(
                "Hall {HallId} pricing: Base rate={BaseRate} SAR/hr, Duration={Duration}hrs, IsWeekend={IsWeekend}, Initial cost={InitialCost}",
                hallId, baseHourlyRate, duration, isWeekend, totalCost);

            return Math.Round(totalCost, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating hall cost for hall {HallId}", hallId);
            throw;
        }
    }

    /// <summary>
    /// Calculate total cost for vendor services
    /// </summary>
    public async Task<decimal> CalculateVendorServicesAsync(List<int> serviceItemIds)
    {
        try
        {
            if (serviceItemIds == null || !serviceItemIds.Any())
                return 0;

            var serviceItems = await _context.ServiceItems
                .Where(s => serviceItemIds.Contains(s.Id) && s.IsAvailable)
                .ToListAsync();

            if (!serviceItems.Any())
            {
                _logger.LogWarning("No valid service items found for IDs: {ServiceItemIds}",
                    string.Join(", ", serviceItemIds));
                return 0;
            }

            var totalCost = serviceItems.Sum(s => s.Price);

            return Math.Round(totalCost, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating vendor services cost");
            throw;
        }
    }

    /// <summary>
    /// Apply discount code and return discount amount
    /// </summary>
    public Task<decimal> ApplyDiscountAsync(decimal amount, string discountCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(discountCode))
                return Task.FromResult(0m);

            // TODO: Implement actual discount code validation from database
            // For now, using hardcoded test codes
            var discount = discountCode.ToUpper() switch
            {
                "WELCOME10" => amount * 0.10m, // 10% off
                "SUMMER20" => amount * 0.20m,  // 20% off
                "VIP25" => amount * 0.25m,     // 25% off
                "FIRSTBOOKING" => Math.Min(amount * 0.15m, 500m), // 15% off, max 500 SAR
                _ => 0m
            };

            _logger.LogInformation("Applied discount code {DiscountCode}: {DiscountAmount} SAR",
                discountCode, discount);

            return Task.FromResult(Math.Round(discount, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount code {DiscountCode}", discountCode);
            return Task.FromResult(0m);
        }
    }

    /// <summary>
    /// Calculate VAT (Value Added Tax) - 15% for Saudi Arabia
    /// </summary>
    public Task<decimal> CalculateVatAsync(decimal amount)
    {
        var vat = amount * VAT_RATE;
        return Task.FromResult(Math.Round(vat, 2));
    }

    /// <summary>
    /// Validate discount code
    /// </summary>
    public Task<(bool IsValid, string Message, decimal DiscountPercentage)> ValidateDiscountCodeAsync(
        string discountCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(discountCode))
                return Task.FromResult<(bool, string, decimal)>((false, "Discount code is required", 0m));

            // TODO: Implement actual discount code validation from database
            // Check expiry date, usage limits, minimum purchase amount, etc.

            var validCodes = new Dictionary<string, (bool Valid, string Message, decimal Percentage)>
            {
                ["WELCOME10"] = (true, "10% discount applied", 0.10m),
                ["SUMMER20"] = (true, "20% summer discount applied", 0.20m),
                ["VIP25"] = (true, "VIP 25% discount applied", 0.25m),
                ["FIRSTBOOKING"] = (true, "First booking 15% discount", 0.15m)
            };

            if (validCodes.TryGetValue(discountCode.ToUpper(), out var result))
                return Task.FromResult(result);

            return Task.FromResult<(bool, string, decimal)>((false, "Invalid discount code", 0m));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating discount code");
            return Task.FromResult<(bool, string, decimal)>((false, "Error validating code", 0m));
        }
    }

    /// <summary>
    /// Get price estimate for booking
    /// </summary>
    public async Task<decimal> GetPriceEstimateAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int numberOfGuests,
        List<int>? serviceItemIds = null)
    {
        try
        {
            var breakdown = await CalculateBookingPriceAsync(
                hallId,
                eventDate,
                startTime,
                endTime,
                serviceItemIds);

            return breakdown.TotalAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price estimate");
            throw;
        }
    }
}
