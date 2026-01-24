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
/// Tabby BNPL (Buy Now Pay Later) payment service
/// Documentation: https://docs.tabby.ai/
/// </summary>
public class TabbyPaymentService : IPaymentProviderService
{
    private readonly TabbySettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TabbyPaymentService> _logger;

    public PaymentProvider Provider => PaymentProvider.Tabby;
    public bool IsEnabled => _settings.Enabled;

    public TabbyPaymentService(
        IOptions<PaymentSettings> paymentSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<TabbyPaymentService> logger)
    {
        _settings = paymentSettings.Value.Tabby;
        _httpClient = httpClientFactory.CreateClient("Tabby");
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.SecretKey}");
    }

    /// <summary>
    /// Create Tabby checkout session
    /// </summary>
    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Tabby checkout for booking {BookingId}, Amount: {Amount} {Currency}",
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

            var tabbyRequest = new TabbyCheckoutRequest
            {
                Payment = new TabbyPayment
                {
                    Amount = request.Amount.ToString("F2"),
                    Currency = request.Currency,
                    Description = request.Description,
                    Buyer = new TabbyBuyer
                    {
                        Phone = request.CustomerPhone,
                        Email = request.CustomerEmail,
                        Name = $"{request.CustomerFirstName} {request.CustomerLastName}".Trim()
                    },
                    ShippingAddress = request.ShippingAddress != null ? new TabbyAddress
                    {
                        City = request.ShippingAddress.City,
                        Address = request.ShippingAddress.Address,
                        Zip = request.ShippingAddress.Zip
                    } : null,
                    Order = new TabbyOrder
                    {
                        TaxAmount = "0.00", // Tax already included in line items
                        ShippingAmount = "0.00",
                        Discount = "0.00",
                        ReferenceId = request.BookingId.ToString(),
                        Items = request.LineItems.Select(item => new TabbyOrderItem
                        {
                            Title = item.Name,
                            Description = item.Description,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice.ToString("F2"),
                            Category = item.Category,
                            Sku = item.Sku
                        }).ToList()
                    },
                    BuyerHistory = new TabbyBuyerHistory
                    {
                        RegisteredSince = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd"),
                        LoyaltyLevel = 0
                    }
                },
                Lang = "ar", // Arabic for Saudi market
                MerchantCode = _settings.MerchantCode,
                MerchantUrls = new TabbyMerchantUrls
                {
                    Success = request.ReturnUrl,
                    Cancel = request.CancelUrl,
                    Failure = request.CancelUrl
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/checkout", tabbyRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tabby checkout failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                var errorResponse = JsonSerializer.Deserialize<TabbyErrorResponse>(responseContent);
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = errorResponse?.Error ?? "Failed to create Tabby checkout",
                    ErrorCode = errorResponse?.ErrorCode ?? "UNKNOWN",
                    RawResponse = responseContent
                };
            }

            var checkoutResponse = JsonSerializer.Deserialize<TabbyCheckoutResponse>(responseContent);

            if (checkoutResponse?.Status != "created")
            {
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = checkoutResponse?.Rejection?.Reason ?? "Checkout not approved",
                    ErrorCode = checkoutResponse?.Rejection?.ReasonCode ?? "REJECTED",
                    RawResponse = responseContent
                };
            }

            _logger.LogInformation("Tabby checkout created: {CheckoutId}", checkoutResponse.Id);

            return new PaymentCheckoutResult
            {
                Success = true,
                CheckoutId = checkoutResponse.Id,
                PaymentUrl = checkoutResponse.Configuration?.AvailableProducts?.Installments?.FirstOrDefault()?.WebUrl ?? "",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Tabby checkout for booking {BookingId}", request.BookingId);
            return new PaymentCheckoutResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Get payment status from Tabby
    /// </summary>
    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string checkoutId)
    {
        try
        {
            _logger.LogInformation("Getting Tabby payment status for checkout {CheckoutId}", checkoutId);

            var response = await _httpClient.GetAsync($"/payments/{checkoutId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tabby status check failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve payment status",
                    RawResponse = responseContent
                };
            }

            var paymentResponse = JsonSerializer.Deserialize<TabbyPaymentResponse>(responseContent);

            var status = MapTabbyStatus(paymentResponse?.Status ?? "");

            return new PaymentStatusResult
            {
                Success = status == "Success",
                Status = status,
                TransactionId = paymentResponse?.Id ?? "",
                Amount = decimal.TryParse(paymentResponse?.Amount, out var amount) ? amount : 0,
                Currency = paymentResponse?.Currency ?? "SAR",
                PaymentMethod = "Tabby",
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Tabby payment status for checkout {CheckoutId}", checkoutId);
            return new PaymentStatusResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Process refund through Tabby
    /// </summary>
    public async Task<PaymentRefundResult> ProcessRefundAsync(string transactionId, decimal amount, string reason)
    {
        try
        {
            _logger.LogInformation("Processing Tabby refund for payment {TransactionId}, Amount: {Amount}",
                transactionId, amount);

            var refundRequest = new
            {
                amount = amount.ToString("F2"),
                reason = reason
            };

            var response = await _httpClient.PostAsJsonAsync($"/payments/{transactionId}/refunds", refundRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Tabby refund failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentRefundResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process refund",
                    RawResponse = responseContent
                };
            }

            var refundResponse = JsonSerializer.Deserialize<TabbyRefundResponse>(responseContent);

            return new PaymentRefundResult
            {
                Success = true,
                RefundId = refundResponse?.Id ?? "",
                Status = "Completed",
                RefundedAmount = amount,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Tabby refund for payment {TransactionId}", transactionId);
            return new PaymentRefundResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validate webhook signature
    /// </summary>
    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_settings.WebhookSecret))
        {
            _logger.LogWarning("Tabby webhook secret not configured");
            return true; // Skip validation if not configured
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToBase64String(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(computedSignature));
    }

    /// <summary>
    /// Parse webhook payload
    /// </summary>
    public PaymentWebhookData ParseWebhookPayload(string payload)
    {
        var webhookData = JsonSerializer.Deserialize<TabbyWebhookPayload>(payload);

        return new PaymentWebhookData
        {
            CheckoutId = webhookData?.Id ?? "",
            TransactionId = webhookData?.Id ?? "",
            Status = MapTabbyStatus(webhookData?.Status ?? ""),
            Amount = decimal.TryParse(webhookData?.Amount, out var amount) ? amount : 0,
            Currency = webhookData?.Currency ?? "SAR",
            PaymentMethod = "Tabby",
            RawPayload = payload
        };
    }

    private string MapTabbyStatus(string tabbyStatus)
    {
        return tabbyStatus.ToLowerInvariant() switch
        {
            "authorized" or "closed" => "Success",
            "created" or "approved" => "Pending",
            "rejected" or "expired" => "Failed",
            "refunded" => "Refunded",
            _ => "Unknown"
        };
    }
}

#region Tabby API Models

internal class TabbyCheckoutRequest
{
    [JsonPropertyName("payment")]
    public TabbyPayment Payment { get; set; } = new();

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "ar";

    [JsonPropertyName("merchant_code")]
    public string MerchantCode { get; set; } = string.Empty;

    [JsonPropertyName("merchant_urls")]
    public TabbyMerchantUrls MerchantUrls { get; set; } = new();
}

internal class TabbyPayment
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("buyer")]
    public TabbyBuyer Buyer { get; set; } = new();

    [JsonPropertyName("shipping_address")]
    public TabbyAddress? ShippingAddress { get; set; }

    [JsonPropertyName("order")]
    public TabbyOrder Order { get; set; } = new();

    [JsonPropertyName("buyer_history")]
    public TabbyBuyerHistory BuyerHistory { get; set; } = new();
}

internal class TabbyBuyer
{
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

internal class TabbyAddress
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("zip")]
    public string Zip { get; set; } = string.Empty;
}

internal class TabbyOrder
{
    [JsonPropertyName("tax_amount")]
    public string TaxAmount { get; set; } = "0.00";

    [JsonPropertyName("shipping_amount")]
    public string ShippingAmount { get; set; } = "0.00";

    [JsonPropertyName("discount")]
    public string Discount { get; set; } = "0.00";

    [JsonPropertyName("reference_id")]
    public string ReferenceId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<TabbyOrderItem> Items { get; set; } = new();
}

internal class TabbyOrderItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public string UnitPrice { get; set; } = "0.00";

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;
}

internal class TabbyBuyerHistory
{
    [JsonPropertyName("registered_since")]
    public string RegisteredSince { get; set; } = string.Empty;

    [JsonPropertyName("loyalty_level")]
    public int LoyaltyLevel { get; set; }
}

internal class TabbyMerchantUrls
{
    [JsonPropertyName("success")]
    public string Success { get; set; } = string.Empty;

    [JsonPropertyName("cancel")]
    public string Cancel { get; set; } = string.Empty;

    [JsonPropertyName("failure")]
    public string Failure { get; set; } = string.Empty;
}

internal class TabbyCheckoutResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("configuration")]
    public TabbyConfiguration? Configuration { get; set; }

    [JsonPropertyName("rejection")]
    public TabbyRejection? Rejection { get; set; }
}

internal class TabbyConfiguration
{
    [JsonPropertyName("available_products")]
    public TabbyAvailableProducts? AvailableProducts { get; set; }
}

internal class TabbyAvailableProducts
{
    [JsonPropertyName("installments")]
    public List<TabbyProduct>? Installments { get; set; }
}

internal class TabbyProduct
{
    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;
}

internal class TabbyRejection
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("reason_code")]
    public string ReasonCode { get; set; } = string.Empty;
}

internal class TabbyPaymentResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";
}

internal class TabbyRefundResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

internal class TabbyErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;
}

internal class TabbyWebhookPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";
}

#endregion
