using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Entities.BookingEntities;

/// <summary>
/// Invoice entity compliant with Saudi Arabia ZATCA (Zakat, Tax and Customs Authority) requirements
/// Reference: https://zatca.gov.sa/en/E-Invoicing/Introduction/Pages/What-is-e-invoicing.aspx
/// </summary>
public class Invoice
{
    public int Id { get; set; }

    // Required ZATCA Fields
    /// <summary>
    /// Unique invoice number in format: INV-YYYY-XXXXXX
    /// </summary>
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice type: Standard, Simplified, or Debit/Credit Note
    /// </summary>
    [Required]
    [StringLength(20)]
    public string InvoiceType { get; set; } = "Standard"; // Standard, Simplified, DebitNote, CreditNote

    /// <summary>
    /// Invoice date and time in ISO 8601 format
    /// </summary>
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Supply/delivery date
    /// </summary>
    public DateTime SupplyDate { get; set; }

    // Relationships
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? HallId { get; set; }
    public Hall? Hall { get; set; }

    // Seller Information (Hall/Platform)
    [Required]
    [StringLength(200)]
    public string SellerName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SellerVatNumber { get; set; } = string.Empty; // 15 digit VAT number

    [Required]
    [StringLength(100)]
    public string SellerCommercialRegistrationNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string SellerAddress { get; set; } = string.Empty;

    [StringLength(10)]
    public string SellerBuildingNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string SellerStreetName { get; set; } = string.Empty;

    [StringLength(100)]
    public string SellerDistrict { get; set; } = string.Empty;

    [StringLength(100)]
    public string SellerCity { get; set; } = string.Empty;

    [StringLength(10)]
    public string SellerPostalCode { get; set; } = string.Empty;

    [StringLength(2)]
    public string SellerCountryCode { get; set; } = "SA"; // ISO 3166-1 alpha-2

    // Buyer Information (Customer)
    [Required]
    [StringLength(200)]
    public string BuyerName { get; set; } = string.Empty;

    [StringLength(100)]
    public string BuyerVatNumber { get; set; } = string.Empty; // Optional for individuals

    [StringLength(500)]
    public string BuyerAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string BuyerCity { get; set; } = string.Empty;

    [StringLength(10)]
    public string BuyerPostalCode { get; set; } = string.Empty;

    [StringLength(2)]
    public string BuyerCountryCode { get; set; } = "SA";

    // Financial Details
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubtotalBeforeTax { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 15.00m; // 15% VAT in Saudi Arabia

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmountWithTax { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "SAR";

    // Payment Information
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, BankTransfer, Online

    [StringLength(20)]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, PartiallyPaid, Cancelled

    public DateTime? PaymentDate { get; set; }

    [StringLength(100)]
    public string PaymentReference { get; set; } = string.Empty;

    // Invoice Line Items
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    // Additional Notes
    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Terms { get; set; } = "Payment due within 30 days of invoice date.";

    // ZATCA E-Invoice Specific Fields
    /// <summary>
    /// QR Code content for simplified invoices (B2C)
    /// </summary>
    [StringLength(1000)]
    public string QRCode { get; set; } = string.Empty;

    /// <summary>
    /// Cryptographic stamp invoice hash for security
    /// </summary>
    [StringLength(500)]
    public string InvoiceHash { get; set; } = string.Empty;

    /// <summary>
    /// UUID for e-invoice reporting to ZATCA
    /// </summary>
    [StringLength(100)]
    public string ZATCA_UUID { get; set; } = string.Empty;

    /// <summary>
    /// Status of invoice submission to ZATCA
    /// </summary>
    [StringLength(20)]
    public string ZATCA_Status { get; set; } = "NotSubmitted"; // NotSubmitted, Submitted, Approved, Rejected

    public DateTime? ZATCA_SubmittedAt { get; set; }

    [StringLength(500)]
    public string ZATCA_Response { get; set; } = string.Empty;

    // Audit Fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsCancelled { get; set; } = false;
    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string CancellationReason { get; set; } = string.Empty;

    // PDF Generation
    [StringLength(500)]
    public string PdfPath { get; set; } = string.Empty;

    public bool IsPdfGenerated { get; set; } = false;
}

/// <summary>
/// Invoice line item for detailed breakdown
/// </summary>
public class InvoiceLineItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string ItemCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; } = 1;

    [StringLength(20)]
    public string Unit { get; set; } = "Service";

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubtotalBeforeTax { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 15.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(50)]
    public string TaxCategory { get; set; } = "Standard"; // Standard, ZeroRated, Exempt
}
