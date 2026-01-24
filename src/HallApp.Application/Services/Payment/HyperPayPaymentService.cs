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
/// HyperPay payment gateway service
/// Documentation: https://wordpresshyperpay.docs.oppwa.com/
/// </summary>
public class HyperPayPaymentService : IPaymentProviderService
{
    private readonly HyperPaySettings _settings;
    private readonly PaymentSettings _paymentSettings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HyperPayPaymentService> _logger;

    public PaymentProvider Provider => PaymentProvider.HyperPay;
    public bool IsEnabled => _settings.Enabled;

    public HyperPayPaymentService(
        IOptions<PaymentSettings> paymentSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<HyperPayPaymentService> logger)
    {
        _paymentSettings = paymentSettings.Value;
        _settings = paymentSettings.Value.HyperPay;
        _httpClient = httpClientFactory.CreateClient("HyperPay");
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken}");
    }

    /// <summary>
    /// Create HyperPay checkout session
    /// </summary>
    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request)
    {
        try
        {
            _logger.LogInformation("Creating HyperPay checkout for booking {BookingId}, Amount: {Amount} {Currency}",
                request.BookingId, request.Amount, request.Currency);

            var formData = new Dictionary<string, string>
            {
                { "entityId", _settings.EntityId },
                { "amount", request.Amount.ToString("F2") },
                { "currency", request.Currency },
                { "paymentType", "DB" }, // Debit (immediate charge)
                { "merchantTransactionId", $"BOOKING-{request.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}" },
                { "customer.email", request.CustomerEmail },
                { "customer.givenName", request.CustomerFirstName },
                { "customer.surname", request.CustomerLastName },
                { "customer.mobile", request.CustomerPhone },
                { "billing.street1", request.BillingAddress?.Address ?? "Saudi Arabia" },
                { "billing.city", request.BillingAddress?.City ?? "Riyadh" },
                { "billing.country", request.BillingAddress?.Country ?? "SA" },
                { "customParameters[bookingId]", request.BookingId.ToString() },
                { "customParameters[description]", request.Description }
            };

            // Add 3D Secure if enabled
            if (_settings.Enable3DSecure)
            {
                formData.Add("threeDSecure.eci", "internet");
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("/v1/checkouts", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HyperPay checkout failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create HyperPay checkout",
                    RawResponse = responseContent
                };
            }

            var checkoutResponse = JsonSerializer.Deserialize<HyperPayCheckoutResponse>(responseContent);

            if (!IsSuccessResult(checkoutResponse?.Result?.Code ?? ""))
            {
                return new PaymentCheckoutResult
                {
                    Success = false,
                    ErrorMessage = checkoutResponse?.Result?.Description ?? "Checkout creation failed",
                    ErrorCode = checkoutResponse?.Result?.Code ?? "UNKNOWN",
                    RawResponse = responseContent
                };
            }

            // Build payment URL with enabled brands
            var brands = string.Join(",", _settings.EnabledPaymentBrands);
            var paymentUrl = $"{_settings.BaseUrl}/v1/paymentWidgets.js?checkoutId={checkoutResponse?.Id}";

            _logger.LogInformation("HyperPay checkout created: {CheckoutId}", checkoutResponse?.Id);

            return new PaymentCheckoutResult
            {
                Success = true,
                CheckoutId = checkoutResponse?.Id ?? "",
                PaymentUrl = paymentUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_paymentSettings.PaymentTimeoutMinutes),
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating HyperPay checkout for booking {BookingId}", request.BookingId);
            return new PaymentCheckoutResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Get payment status from HyperPay
    /// </summary>
    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string checkoutId)
    {
        try
        {
            _logger.LogInformation("Getting HyperPay payment status for checkout {CheckoutId}", checkoutId);

            var response = await _httpClient.GetAsync($"/v1/checkouts/{checkoutId}/payment?entityId={_settings.EntityId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HyperPay status check failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentStatusResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve payment status",
                    RawResponse = responseContent
                };
            }

            var paymentResponse = JsonSerializer.Deserialize<HyperPayPaymentResponse>(responseContent);
            var resultCode = paymentResponse?.Result?.Code ?? "";
            var status = MapHyperPayStatus(resultCode);

            return new PaymentStatusResult
            {
                Success = status == "Success",
                Status = status,
                TransactionId = paymentResponse?.Id ?? "",
                Amount = decimal.TryParse(paymentResponse?.Amount, out var amount) ? amount : 0,
                Currency = paymentResponse?.Currency ?? "SAR",
                PaymentMethod = paymentResponse?.PaymentBrand ?? "Card",
                CardLast4 = paymentResponse?.Card?.Last4Digits ?? "",
                CardBrand = paymentResponse?.Card?.Brand ?? "",
                CardExpiry = !string.IsNullOrEmpty(paymentResponse?.Card?.ExpiryMonth)
                    ? $"{paymentResponse.Card.ExpiryMonth}/{paymentResponse.Card.ExpiryYear}"
                    : "",
                ErrorMessage = paymentResponse?.Result?.Description ?? "",
                ErrorCode = resultCode,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting HyperPay payment status for checkout {CheckoutId}", checkoutId);
            return new PaymentStatusResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Process refund through HyperPay
    /// </summary>
    public async Task<PaymentRefundResult> ProcessRefundAsync(string transactionId, decimal amount, string reason)
    {
        try
        {
            _logger.LogInformation("Processing HyperPay refund for transaction {TransactionId}, Amount: {Amount}",
                transactionId, amount);

            var formData = new Dictionary<string, string>
            {
                { "entityId", _settings.EntityId },
                { "amount", amount.ToString("F2") },
                { "currency", "SAR" },
                { "paymentType", "RF" } // Refund
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync($"/v1/payments/{transactionId}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HyperPay refund failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PaymentRefundResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process refund",
                    RawResponse = responseContent
                };
            }

            var refundResponse = JsonSerializer.Deserialize<HyperPayRefundResponse>(responseContent);

            if (!IsSuccessResult(refundResponse?.Result?.Code ?? ""))
            {
                return new PaymentRefundResult
                {
                    Success = false,
                    ErrorMessage = refundResponse?.Result?.Description ?? "Refund failed",
                    ErrorCode = refundResponse?.Result?.Code ?? "UNKNOWN",
                    RawResponse = responseContent
                };
            }

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
            _logger.LogError(ex, "Error processing HyperPay refund for transaction {TransactionId}", transactionId);
            return new PaymentRefundResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Validate webhook signature using HMAC-SHA256
    /// </summary>
    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_settings.WebhookSecret))
        {
            _logger.LogWarning("HyperPay webhook secret not configured");
            return true; // Skip validation if not configured
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(computedSignature));
    }

    /// <summary>
    /// Parse webhook payload
    /// </summary>
    public PaymentWebhookData ParseWebhookPayload(string payload)
    {
        var webhookData = JsonSerializer.Deserialize<HyperPayWebhookPayload>(payload);

        return new PaymentWebhookData
        {
            CheckoutId = webhookData?.CheckoutId ?? "",
            TransactionId = webhookData?.Id ?? "",
            Status = MapHyperPayStatus(webhookData?.Result?.Code ?? ""),
            Amount = decimal.TryParse(webhookData?.Amount, out var amount) ? amount : 0,
            Currency = webhookData?.Currency ?? "SAR",
            PaymentMethod = webhookData?.PaymentBrand ?? "Card",
            RawPayload = payload,
            AdditionalData = new Dictionary<string, object>
            {
                { "result_code", webhookData?.Result?.Code ?? "" },
                { "result_description", webhookData?.Result?.Description ?? "" }
            }
        };
    }

    private bool IsSuccessResult(string resultCode)
    {
        // HyperPay success codes
        return resultCode.StartsWith("000.000.") ||
               resultCode.StartsWith("000.100.1") ||
               resultCode.StartsWith("000.3") ||
               resultCode.StartsWith("000.6");
    }

    private bool IsPendingResult(string resultCode)
    {
        // HyperPay pending codes
        return resultCode.StartsWith("000.200") || // pending
               resultCode.StartsWith("800.400");   // review
    }

    private string MapHyperPayStatus(string resultCode)
    {
        if (IsSuccessResult(resultCode)) return "Success";
        if (IsPendingResult(resultCode)) return "Pending";
        return "Failed";
    }
}

#region HyperPay API Models

internal class HyperPayCheckoutResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public HyperPayResult? Result { get; set; }
}

internal class HyperPayResult
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

internal class HyperPayPaymentResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";

    [JsonPropertyName("paymentBrand")]
    public string PaymentBrand { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public HyperPayResult? Result { get; set; }

    [JsonPropertyName("card")]
    public HyperPayCard? Card { get; set; }
}

internal class HyperPayCard
{
    [JsonPropertyName("last4Digits")]
    public string Last4Digits { get; set; } = string.Empty;

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("expiryMonth")]
    public string ExpiryMonth { get; set; } = string.Empty;

    [JsonPropertyName("expiryYear")]
    public string ExpiryYear { get; set; } = string.Empty;
}

internal class HyperPayRefundResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public HyperPayResult? Result { get; set; }
}

internal class HyperPayWebhookPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("checkoutId")]
    public string CheckoutId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0.00";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";

    [JsonPropertyName("paymentBrand")]
    public string PaymentBrand { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public HyperPayResult? Result { get; set; }
}

#endregion
