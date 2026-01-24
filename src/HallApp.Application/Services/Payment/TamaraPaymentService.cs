using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HallApp.Application.Configuration;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HallApp.Application.Services.Payment;

/// <summary>
/// Tamara BNPL (Buy Now Pay Later) payment service
/// Documentation: https://docs.tamara.co/
/// </summary>
public class TamaraPaymentService : IPaymentProviderService
{
    private readonly TamaraSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TamaraPaymentService> _logger;

    public PaymentProvider Provider => PaymentProvider.Tamara;
    public bool IsEnabled => _settings.Enabled;

    public TamaraPaymentService(
        IOptions<PaymentSettings> paymentSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<TamaraPaymentService> logger)
    {
        _settings = paymentSettings.Value.Tamara;
        _httpClient = httpClientFactory.CreateClient("Tamara");
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiToken}");
    }

    /// <summary>
    /// Create Tamara checkout session
    /// </summary>
    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Tamara checkout for booking {BookingId}, Amount: {Amount} {Currency}",
                request.BookingId, request.Amount, request.Currency);

            // Validate amount limits
            if (request.Amount < _settings.MinOrderAmount)
            {
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = $"Order amount must be at least {_settings.MinOrderAmount} {request.Currency}",
                    ErrorCode = "MIN_AMOUNT_NOT_MET"
                };
            }

            if (request.Amount > _settings.MaxOrderAmount)
            {
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = $"Order amount exceeds maximum of {_settings.MaxOrderAmount} {request.Currency}",
                    ErrorCode = "MAX_AMOUNT_EXCEEDED"
                };
            }

            // Calculate tax amount (15% VAT in Saudi Arabia)
            var taxAmount = Math.Round(request.Amount * 0.15m / 1.15m, 2);
            var subtotal = request.Amount - taxAmount;

            var tamaraRequest = new TamaraCheckoutRequest
            {
                OrderReferenceId = request.BookingId.ToString(),
                TotalAmount = new TamaraAmount
                {
                    Amount = request.Amount,
                    Currency = request.Currency
                },
                Description = request.Description,
                CountryCode = _settings.CountryCode,
                PaymentType = _settings.PaymentTypes.FirstOrDefault() ?? "PAY_BY_INSTALMENTS",
                Locale = "ar_SA",
                Items = request.LineItems.Select(item => new TamaraItem
                {
                    ReferenceId = item.Sku,
                    Type = "Digital", // Event services are digital
                    Name = item.Name,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    UnitPrice = new TamaraAmount
                    {
                        Amount = item.UnitPrice,
                        Currency = request.Currency
                    },
                    TotalAmount = new TamaraAmount
                    {
                        Amount = item.TotalPrice,
                        Currency = request.Currency
                    }
                }).ToList(),
                Consumer = new TamaraConsumer
                {
                    FirstName = request.CustomerFirstName,
                    LastName = request.CustomerLastName,
                    PhoneNumber = request.CustomerPhone,
                    Email = request.CustomerEmail
                },
                ShippingAddress = request.ShippingAddress != null ? new TamaraAddress
                {
                    FirstName = request.CustomerFirstName,
                    LastName = request.CustomerLastName,
                    Line1 = request.ShippingAddress.Address,
                    City = request.ShippingAddress.City,
                    CountryCode = request.ShippingAddress.Country,
                    PhoneNumber = request.CustomerPhone
                } : new TamaraAddress
                {
                    FirstName = request.CustomerFirstName,
                    LastName = request.CustomerLastName,
                    Line1 = "Saudi Arabia",
                    City = "Riyadh",
                    CountryCode = "SA",
                    PhoneNumber = request.CustomerPhone
                },
                TaxAmount = new TamaraAmount
                {
                    Amount = taxAmount,
                    Currency = request.Currency
                },
                ShippingAmount = new TamaraAmount
                {
                    Amount = 0,
                    Currency = request.Currency
                },
                MerchantUrl = new TamaraMerchantUrl
                {
                    Success = request.ReturnUrl,
                    Failure = request.CancelUrl,
                    Cancel = request.CancelUrl,
                    Notification = request.WebhookUrl
                },
                Platform = "HallApp"
            };

            var response = await _httpClient.PostAsJsonAsync("/checkout", tamaraRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tamara checkout failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                var errorResponse = JsonSerializer.Deserialize<TamaraErrorResponse>(responseContent);
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = errorResponse?.Message ?? "Failed to create Tamara checkout",
                    ErrorCode = errorResponse?.ErrorType ?? "UNKNOWN",
                    RawResponse = responseContent
                };
            }

            var checkoutResponse = JsonSerializer.Deserialize<TamaraCheckoutResponse>(responseContent);

            if (string.IsNullOrEmpty(checkoutResponse?.CheckoutUrl))
            {
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = "No checkout URL returned from Tamara",
                    ErrorCode = "NO_CHECKOUT_URL",
                    RawResponse = responseContent
                };
            }

            _logger.LogInformation("Tamara checkout created: {OrderId}", checkoutResponse.OrderId);

            return new PaymentCheckoutResult
            {
                Success = true,
                CheckoutId = checkoutResponse.OrderId,
                PaymentUrl = checkoutResponse.CheckoutUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Tamara checkout for booking {BookingId}", request.BookingId);
            return new PaymentCheckoutResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Get payment status from Tamara
    /// </summary>
    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string checkoutId)
    {
        try
        {
            _logger.LogInformation("Getting Tamara payment status for order {OrderId}", checkoutId);

            var response = await _httpClient.GetAsync($"/orders/{checkoutId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tamara status check failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve payment status",
                    RawResponse = responseContent
                };
            }

            var orderResponse = JsonSerializer.Deserialize<TamaraOrderResponse>(responseContent);

            var status = MapTamaraStatus(orderResponse?.Status ?? "");

            return new PaymentStatusResult
            {
                Success = status == "Success",
                Status = status,
                TransactionId = orderResponse?.OrderId ?? "",
                Amount = orderResponse?.TotalAmount?.Amount ?? 0,
                Currency = orderResponse?.TotalAmount?.Currency ?? "SAR",
                PaymentMethod = "Tamara",
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Tamara payment status for order {OrderId}", checkoutId);
            return new PaymentStatusResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Process refund through Tamara
    /// </summary>
    public async Task<PaymentRefundResult> ProcessRefundAsync(string transactionId, decimal amount, string reason)
    {
        try
        {
            _logger.LogInformation("Processing Tamara refund for order {OrderId}, Amount: {Amount}",
                transactionId, amount);

            var refundRequest = new TamaraRefundRequest
            {
                TotalAmount = new TamaraAmount
                {
                    Amount = amount,
                    Currency = "SAR"
                },
                Comment = reason
            };

            var response = await _httpClient.PostAsJsonAsync($"/orders/{transactionId}/refunds", refundRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tamara refund failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentRefundResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process refund",
                    RawResponse = responseContent
                };
            }

            var refundResponse = JsonSerializer.Deserialize<TamaraRefundResponse>(responseContent);

            return new PaymentRefundResult
            {
                Success = true,
                RefundId = refundResponse?.RefundId ?? "",
                Status = "Completed",
                RefundedAmount = amount,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Tamara refund for order {OrderId}", transactionId);
            return new PaymentRefundResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validate webhook signature (Tamara uses notification token)
    /// </summary>
    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_settings.NotificationToken))
        {
            _logger.LogWarning("Tamara notification token not configured");
            return true; // Skip validation if not configured
        }

        // Tamara sends the notification token in header for validation
        return signature == _settings.NotificationToken;
    }

    /// <summary>
    /// Parse webhook payload
    /// </summary>
    public PaymentWebhookData ParseWebhookPayload(string payload)
    {
        var webhookData = JsonSerializer.Deserialize<TamaraWebhookPayload>(payload);

        return new PaymentWebhookData
        {
            CheckoutId = webhookData?.OrderId ?? "",
            TransactionId = webhookData?.OrderId ?? "",
            Status = MapTamaraStatus(webhookData?.OrderStatus ?? ""),
            Amount = webhookData?.TotalAmount?.Amount ?? 0,
            Currency = webhookData?.TotalAmount?.Currency ?? "SAR",
            PaymentMethod = "Tamara",
            RawPayload = payload,
            AdditionalData = new Dictionary<string, object>
            {
                { "order_reference_id", webhookData?.OrderReferenceId ?? "" },
                { "event_type", webhookData?.EventType ?? "" }
            }
        };
    }

    /// <summary>
    /// Authorize/Capture payment after successful checkout
    /// Tamara requires explicit authorization after customer approves
    /// </summary>
    public async Task<bool> AuthorizePaymentAsync(string orderId)
    {
        try
        {
            _logger.LogInformation("Authorizing Tamara payment for order {OrderId}", orderId);

            var response = await _httpClient.PostAsync($"/orders/{orderId}/authorise", null);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tamara authorize failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return false;
            }

            _logger.LogInformation("Tamara payment authorized for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authorizing Tamara payment for order {OrderId}", orderId);
            return false;
        }
    }

    /// <summary>
    /// Capture authorized payment
    /// </summary>
    public async Task<bool> CapturePaymentAsync(string orderId, decimal amount)
    {
        try
        {
            _logger.LogInformation("Capturing Tamara payment for order {OrderId}, Amount: {Amount}", orderId, amount);

            var captureRequest = new TamaraCaptureRequest
            {
                TotalAmount = new TamaraAmount
                {
                    Amount = amount,
                    Currency = "SAR"
                },
                ShippingInfo = new TamaraShippingInfo
                {
                    ShippedAt = DateTime.UtcNow.ToString("O"),
                    ShippingCompany = "Digital Delivery"
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"/orders/{orderId}/capture", captureRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tamara capture failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                return false;
            }

            _logger.LogInformation("Tamara payment captured for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing Tamara payment for order {OrderId}", orderId);
            return false;
        }
    }

    private string MapTamaraStatus(string tamaraStatus)
    {
        return tamaraStatus.ToLowerInvariant() switch
        {
            "approved" or "captured" or "fully_captured" => "Success",
            "new" or "pending" or "authorised" => "Pending",
            "declined" or "expired" or "canceled" or "fully_refunded" => "Failed",
            "refunded" or "partially_refunded" => "Refunded",
            _ => "Unknown"
        };
    }
}

#region Tamara API Models

internal class TamaraCheckoutRequest
{
    [JsonPropertyName("order_reference_id")]
    public string OrderReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public TamaraAmount TotalAmount { get; set; } = new();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = "SA";

    [JsonPropertyName("payment_type")]
    public string PaymentType { get; set; } = "PAY_BY_INSTALMENTS";

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "ar_SA";

    [JsonPropertyName("items")]
    public List<TamaraItem> Items { get; set; } = new();

    [JsonPropertyName("consumer")]
    public TamaraConsumer Consumer { get; set; } = new();

    [JsonPropertyName("shipping_address")]
    public TamaraAddress ShippingAddress { get; set; } = new();

    [JsonPropertyName("tax_amount")]
    public TamaraAmount TaxAmount { get; set; } = new();

    [JsonPropertyName("shipping_amount")]
    public TamaraAmount ShippingAmount { get; set; } = new();

    [JsonPropertyName("merchant_url")]
    public TamaraMerchantUrl MerchantUrl { get; set; } = new();

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "HallApp";
}

internal class TamaraAmount
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";
}

internal class TamaraItem
{
    [JsonPropertyName("reference_id")]
    public string ReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Digital";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public TamaraAmount UnitPrice { get; set; } = new();

    [JsonPropertyName("total_amount")]
    public TamaraAmount TotalAmount { get; set; } = new();
}

internal class TamaraConsumer
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

internal class TamaraAddress
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("line1")]
    public string Line1 { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = "SA";

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

internal class TamaraMerchantUrl
{
    [JsonPropertyName("success")]
    public string Success { get; set; } = string.Empty;

    [JsonPropertyName("failure")]
    public string Failure { get; set; } = string.Empty;

    [JsonPropertyName("cancel")]
    public string Cancel { get; set; } = string.Empty;

    [JsonPropertyName("notification")]
    public string Notification { get; set; } = string.Empty;
}

internal class TamaraCheckoutResponse
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("checkout_url")]
    public string CheckoutUrl { get; set; } = string.Empty;
}

internal class TamaraOrderResponse
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public TamaraAmount? TotalAmount { get; set; }
}

internal class TamaraRefundRequest
{
    [JsonPropertyName("total_amount")]
    public TamaraAmount TotalAmount { get; set; } = new();

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}

internal class TamaraRefundResponse
{
    [JsonPropertyName("refund_id")]
    public string RefundId { get; set; } = string.Empty;
}

internal class TamaraCaptureRequest
{
    [JsonPropertyName("total_amount")]
    public TamaraAmount TotalAmount { get; set; } = new();

    [JsonPropertyName("shipping_info")]
    public TamaraShippingInfo ShippingInfo { get; set; } = new();
}

internal class TamaraShippingInfo
{
    [JsonPropertyName("shipped_at")]
    public string ShippedAt { get; set; } = string.Empty;

    [JsonPropertyName("shipping_company")]
    public string ShippingCompany { get; set; } = string.Empty;
}

internal class TamaraErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error_type")]
    public string ErrorType { get; set; } = string.Empty;
}

internal class TamaraWebhookPayload
{
    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("order_reference_id")]
    public string OrderReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("total_amount")]
    public TamaraAmount? TotalAmount { get; set; }
}

#endregion
