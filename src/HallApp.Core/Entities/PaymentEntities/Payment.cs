using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Entities.PaymentEntities;

/// <summary>
/// Payment entity for HyperPay integration
/// </summary>
public class Payment
{
    public int Id { get; set; }

    // Booking relationship
    [Required]
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    // HyperPay specific fields
    [Required]
    [StringLength(100)]
    public string CheckoutId { get; set; } = string.Empty;

    [StringLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PaymentGateway { get; set; } = "HyperPay";

    [Required]
    [StringLength(20)]
    public string PaymentBrand { get; set; } = string.Empty;  // VISA, MASTER, MADA, APPLEPAY

    // Financial details
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "SAR";

    // Payment status
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";  // Pending, Success, Failed, Refunded

    [StringLength(200)]
    public string StatusDescription { get; set; } = string.Empty;

    [StringLength(10)]
    public string ResultCode { get; set; } = string.Empty;

    // Card details (for record keeping - never store full card number)
    [StringLength(20)]
    public string CardBrand { get; set; } = string.Empty;

    [StringLength(4)]
    public string Last4Digits { get; set; } = string.Empty;

    [StringLength(7)]
    public string CardExpiryDate { get; set; } = string.Empty;  // MM/YYYY

    [StringLength(100)]
    public string CardHolder { get; set; } = string.Empty;

    // Customer information
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [StringLength(100)]
    public string CustomerEmail { get; set; } = string.Empty;

    [StringLength(50)]
    public string CustomerPhone { get; set; } = string.Empty;

    // Risk management
    [StringLength(50)]
    public string RiskScore { get; set; } = string.Empty;

    [StringLength(200)]
    public string IpAddress { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }

    // Webhook data (for audit trail)
    [Column(TypeName = "text")]
    public string WebhookPayload { get; set; } = string.Empty;

    [StringLength(500)]
    public string FailureReason { get; set; } = string.Empty;

    // Refund support
    public bool IsRefunded { get; set; } = false;
    public DateTime? RefundedAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundAmount { get; set; } = 0;

    [StringLength(500)]
    public string RefundReason { get; set; } = string.Empty;

    public int? RefundedBy { get; set; }  // Admin user who processed refund

    // Navigation
    public List<PaymentRefund> Refunds { get; set; } = new();
}

/// <summary>
/// Payment refund tracking
/// </summary>
public class PaymentRefund
{
    public int Id { get; set; }

    [Required]
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundAmount { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";  // Pending, Completed, Failed

    [StringLength(100)]
    public string RefundTransactionId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public int RequestedBy { get; set; }  // Admin user ID
    public AppUser? RequestedByUser { get; set; }

    [Column(TypeName = "text")]
    public string ResponsePayload { get; set; } = string.Empty;
}
