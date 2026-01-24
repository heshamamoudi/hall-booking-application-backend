#nullable enable

namespace HallApp.Application.DTOs.Payment;

public class PaymentCheckoutRequestDto
{
    public int BookingId { get; set; }
    public string? Provider { get; set; } // hyperpay, tabby, tamara
    public string? PaymentBrand { get; set; } // VISA, MASTER, MADA, APPLEPAY, STC_PAY
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class PaymentCheckoutResponseDto
{
    public string CheckoutId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool TestMode { get; set; }
    public string Provider { get; set; } = string.Empty;
}

public class PaymentStatusResponseDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Success, Failed
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentBrand { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? FailureReason { get; set; }
}

public class PaymentRefundRequestDto
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class PaymentRefundResponseDto
{
    public int RefundId { get; set; }
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class HyperPayWebhookDto
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? ReferencedId { get; set; }
    public WebhookResultDto? Result { get; set; }
}

public class WebhookResultDto
{
    public string? Code { get; set; }
    public string? Description { get; set; }
}

public class PaymentListDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentBrand { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsRefunded { get; set; }
}

public class PaymentDetailsDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string CheckoutId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentGateway { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentBrand { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Last4Digits { get; set; } = string.Empty;
    public string CardHolder { get; set; } = string.Empty;
    public string CardExpiryMonth { get; set; } = string.Empty;
    public string CardExpiryYear { get; set; } = string.Empty;
    public string ResultCode { get; set; } = string.Empty;
    public string ResultDescription { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRefunded { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public List<PaymentRefundDto> Refunds { get; set; } = new();
}

public class PaymentRefundDto
{
    public int Id { get; set; }
    public string RefundTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
