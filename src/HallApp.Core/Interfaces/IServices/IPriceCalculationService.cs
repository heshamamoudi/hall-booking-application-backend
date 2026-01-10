namespace HallApp.Core.Interfaces.IServices;

public interface IPriceCalculationService
{
    Task<BookingPriceBreakdown> CalculateBookingPriceAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime,
        List<int>? serviceItemIds = null,
        string? discountCode = null);

    Task<decimal> CalculateHallCostAsync(int hallId, DateTime eventDate, TimeSpan startTime, TimeSpan endTime);

    Task<decimal> CalculateVendorServicesAsync(List<int> serviceItemIds);

    Task<decimal> ApplyDiscountAsync(decimal amount, string discountCode);

    Task<decimal> CalculateVatAsync(decimal amount);

    Task<(bool IsValid, string Message, decimal DiscountPercentage)> ValidateDiscountCodeAsync(string discountCode);

    Task<decimal> GetPriceEstimateAsync(
        int hallId,
        DateTime eventDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int numberOfGuests,
        List<int>? serviceItemIds = null);
}

/// <summary>
/// Comprehensive pricing breakdown for a booking
/// </summary>
public class BookingPriceBreakdown
{
    public decimal HallCost { get; set; }
    public decimal VendorServicesCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountCode { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
}
