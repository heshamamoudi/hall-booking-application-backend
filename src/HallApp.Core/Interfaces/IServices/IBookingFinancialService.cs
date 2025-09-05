namespace HallApp.Core.Interfaces.IServices;

public class BookingServiceRequest
{
    public int ServiceItemId { get; set; }
    public int VendorId { get; set; }
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
}

public class BookingFinancialResult
{
    public decimal HallCost { get; set; }
    public decimal VendorServicesCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public List<VendorFinancialResult> VendorBreakdown { get; set; } = new();
}

public class VendorFinancialResult
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<ServiceFinancialResult> Services { get; set; } = new();
}

public class ServiceFinancialResult
{
    public int ServiceItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public interface IBookingFinancialService
{
    Task<BookingFinancialResult> CalculateBookingFinancialsAsync(
        int hallId,
        DateTime eventStart,
        DateTime eventEnd,
        List<BookingServiceRequest> services,
        string discountCode = "",
        string region = "Riyadh");
}
