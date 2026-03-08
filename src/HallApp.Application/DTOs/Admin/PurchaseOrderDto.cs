namespace HallApp.Application.DTOs.Admin;

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int BookingId { get; set; }
    public string SupplierType { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierVatNumber { get; set; } = string.Empty;
    public string SupplierCommercialRegistrationNumber { get; set; } = string.Empty;
    public string SupplierAddress { get; set; } = string.Empty;
    public string SupplierBankIban { get; set; } = string.Empty;
    public string SupplierBankName { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Status { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdatePurchaseOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class RecordPurchaseOrderPaymentDto
{
    public string PaymentReference { get; set; } = string.Empty;
}
