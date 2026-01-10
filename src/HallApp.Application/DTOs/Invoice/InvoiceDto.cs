namespace HallApp.Application.DTOs.Invoice;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime SupplyDate { get; set; }
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? HallId { get; set; }
    public string HallName { get; set; } = string.Empty;

    // Seller Information
    public string SellerName { get; set; } = string.Empty;
    public string SellerVatNumber { get; set; } = string.Empty;
    public string SellerCommercialRegistrationNumber { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerCity { get; set; } = string.Empty;

    // Buyer Information
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerVatNumber { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerCity { get; set; } = string.Empty;

    // Financial Details
    public decimal SubtotalBeforeTax { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmountWithTax { get; set; }
    public string Currency { get; set; } = string.Empty;

    // Payment Information
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;

    // Line Items
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();

    // Additional Info
    public string Notes { get; set; } = string.Empty;
    public string Terms { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;

    // ZATCA Status
    public string ZATCA_Status { get; set; } = string.Empty;
    public DateTime? ZATCA_SubmittedAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string CancellationReason { get; set; } = string.Empty;

    // PDF
    public string PdfPath { get; set; } = string.Empty;
    public bool IsPdfGenerated { get; set; }
}

public class InvoiceLineItemDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubtotalBeforeTax { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string TaxCategory { get; set; } = string.Empty;
}

public class CreateInvoiceDto
{
    public int BookingId { get; set; }
    public string InvoiceType { get; set; } = "Standard";
    public DateTime SupplyDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool AutoGeneratePdf { get; set; } = true;
}

public class InvoiceListDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public decimal TotalAmountWithTax { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string ZATCA_Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
