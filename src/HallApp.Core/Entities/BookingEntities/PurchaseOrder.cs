using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities.BookingEntities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;

    // Relationships
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    // Supplier Info
    public string SupplierType { get; set; } = string.Empty; // "Hall" or "Vendor"
    public int SupplierId { get; set; }
    public int? SupplierOrganizationId { get; set; }
    public Organization? SupplierOrganization { get; set; }

    // Denormalized Supplier Details (snapshot at PO creation)
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierVatNumber { get; set; } = string.Empty;
    public string SupplierCommercialRegistrationNumber { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string SupplierBankIban { get; set; } = string.Empty;
    public string SupplierBankName { get; set; } = string.Empty;

    // Financial
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; }
    [Column(TypeName = "decimal(5,4)")]
    public decimal CommissionRate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }
    [Column(TypeName = "decimal(5,4)")]
    public decimal TaxRate { get; set; } = 0.15m;
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";

    // Status
    public string Status { get; set; } = "Draft";

    // Payment Tracking
    public DateTime? PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
