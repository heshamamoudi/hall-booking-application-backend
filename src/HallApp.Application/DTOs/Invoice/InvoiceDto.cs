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

    // Refund Information
    public decimal RefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    public string RefundMethod { get; set; } = string.Empty;
    public DateTime? RefundDate { get; set; }

    // Regeneration Tracking
    public bool IsRegenerated { get; set; }
    public DateTime? RegeneratedAt { get; set; }
    public int RegenerationCount { get; set; }

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
    public bool IsCancelled { get; set; }
    public bool IsRegenerated { get; set; }
    public decimal RefundAmount { get; set; }
}

/// <summary>
/// Request DTO for bulk invoice generation
/// </summary>
public class BulkInvoiceGenerateRequestDto
{
    public List<int> BookingIds { get; set; } = new();
}

/// <summary>
/// Response DTO for bulk invoice generation results
/// </summary>
public class BulkInvoiceGenerateResponseDto
{
    public List<InvoiceDto> Generated { get; set; } = new();
    public List<BulkInvoiceFailureDto> Failed { get; set; } = new();
}

/// <summary>
/// Failure detail for a single booking during bulk generation
/// </summary>
public class BulkInvoiceFailureDto
{
    public int BookingId { get; set; }
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for bulk QR code regeneration results
/// </summary>
public class BulkQRRegenerateResponseDto
{
    public int Updated { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Request DTO for cancelling an invoice with enhanced validation
/// </summary>
public class CancelInvoiceRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for refunding an invoice
/// </summary>
public class RefundInvoiceRequestDto
{
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RefundMethod { get; set; } = string.Empty;
}

/// <summary>
/// Invoice statistics summary DTO for the invoice page redesign.
/// Role-scoped: Admin sees all, HallOrganizationManager sees org halls, HallManager sees assigned halls.
/// </summary>
public class InvoiceStatisticsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal TaxCollected { get; set; }
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int CancelledInvoices { get; set; }
}

/// <summary>
/// Filter parameters for the enhanced invoice list endpoint.
/// All filters are optional; when null/empty, the filter is not applied.
/// </summary>
public class InvoiceFilterDto
{
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public int? HallId { get; set; }
    public int? OrganizationId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsCancelled { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "InvoiceDate";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Request DTO for bulk invoice export. Supports CSV and Excel formats.
/// </summary>
public class InvoiceExportRequestDto
{
    public List<int> InvoiceIds { get; set; } = new();
    public string Format { get; set; } = "csv";
}

/// <summary>
/// Enhanced invoice list DTO with additional fields for the redesigned invoice page.
/// Adds PaymentMethod and BookingId to the existing InvoiceListDto fields.
/// </summary>
public class InvoiceListEnhancedDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public decimal TotalAmountWithTax { get; set; }
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ZATCA_Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsRegenerated { get; set; }
    public decimal RefundAmount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
