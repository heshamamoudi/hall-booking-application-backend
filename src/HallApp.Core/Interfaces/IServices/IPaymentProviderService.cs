using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.PaymentEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Supported payment providers
/// </summary>
public enum PaymentProvider
{
    HyperPay,
    Tabby,
    Tamara
}

/// <summary>
/// Interface for payment provider services - allows multiple payment gateways
/// </summary>
public interface IPaymentProviderService
{
    /// <summary>
    /// Provider identifier
    /// </summary>
    PaymentProvider Provider { get; }

    /// <summary>
    /// Check if provider is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Create checkout session
    /// </summary>
    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request);

    /// <summary>
    /// Get payment status from provider
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string checkoutId);

    /// <summary>
    /// Process refund
    /// </summary>
    Task<PaymentRefundResult> ProcessRefundAsync(string transactionId, decimal amount, string reason);

    /// <summary>
    /// Validate webhook signature
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature);

    /// <summary>
    /// Parse webhook payload
    /// </summary>
    PaymentWebhookData ParseWebhookPayload(string payload);
}

/// <summary>
/// Payment checkout request
/// </summary>
public class PaymentCheckoutRequest
{
    public int BookingId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;

    // For BNPL providers (Tabby/Tamara)
    public List<PaymentLineItem> LineItems { get; set; } = new();
    public PaymentShippingAddress? ShippingAddress { get; set; }
    public PaymentBillingAddress? BillingAddress { get; set; }
}

/// <summary>
/// Line item for BNPL providers
/// </summary>
public class PaymentLineItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Category { get; set; } = "Service";
    public string Sku { get; set; } = string.Empty;
}

/// <summary>
/// Shipping address for BNPL providers
/// </summary>
public class PaymentShippingAddress
{
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = "SA";
}

/// <summary>
/// Billing address for BNPL providers
/// </summary>
public class PaymentBillingAddress
{
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = "SA";
}

/// <summary>
/// Checkout result from provider
/// </summary>
public class PaymentCheckoutResult
{
    public bool Success { get; set; }
    public string CheckoutId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}

/// <summary>
/// Payment status result from provider
/// </summary>
public class PaymentStatusResult
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Success, Failed, Expired, Cancelled
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string PaymentMethod { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;

    // Card details (if applicable)
    public string CardLast4 { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string CardExpiry { get; set; } = string.Empty;
}

/// <summary>
/// Refund result from provider
/// </summary>
public class PaymentRefundResult
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RefundedAmount { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}

/// <summary>
/// Webhook data parsed from provider
/// </summary>
public class PaymentWebhookData
{
    public string CheckoutId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string PaymentMethod { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
