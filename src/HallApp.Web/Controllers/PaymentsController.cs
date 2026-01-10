using HallApp.Application.DTOs.Payment;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Infrastructure.Data;
using HallApp.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HallApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly DataContext _context;
    private readonly ILogger<PaymentsController> _logger;
    private readonly HttpClient _httpClient;
    private readonly HyperPaySettings _settings;

    public PaymentsController(
        DataContext context,
        ILogger<PaymentsController> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<HyperPaySettings> settings)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("HyperPay");
        _settings = settings.Value;
    }

    /// <summary>
    /// Initialize payment checkout - Creates a checkout session with HyperPay
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<ActionResult<PaymentCheckoutResponseDto>> InitiateCheckout(
        [FromBody] PaymentCheckoutRequestDto request)
    {
        try
        {
            // Validate booking exists and is in correct status
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                    .ThenInclude(c => c.AppUser)
                .Include(b => b.Hall)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found" });
            }

            if (booking.Status == "Cancelled")
            {
                return BadRequest(new { message = "Cannot process payment for cancelled booking" });
            }

            // Check if payment already exists for this booking
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == request.BookingId && p.Status == "Success");

            if (existingPayment != null)
            {
                return BadRequest(new { message = "Payment already completed for this booking" });
            }

            // Prepare HyperPay checkout request
            var checkoutData = new Dictionary<string, string>
            {
                { "entityId", _settings.EntityId },
                { "amount", booking.TotalAmount.ToString("F2") },
                { "currency", _settings.Currency },
                { "paymentType", "DB" }, // Debit (immediate capture)
                { "customer.email", booking.Customer?.AppUser?.Email ?? "" },
                { "customer.givenName", booking.Customer?.AppUser?.FirstName ?? "" },
                { "customer.surname", booking.Customer?.AppUser?.LastName ?? "" },
                { "customer.mobile", booking.Customer?.AppUser?.PhoneNumber ?? "" },
                { "billing.country", "SA" },
                { "merchantTransactionId", $"BOOKING-{booking.Id}-{DateTime.UtcNow.Ticks}" },
                { "customParameters[BOOKING_ID]", booking.Id.ToString() },
                { "customParameters[HALL_ID]", booking.HallId.ToString() },
                { "customParameters[CUSTOMER_ID]", booking.CustomerId.ToString() },
                { "risk.parameters[USER_IP]", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "" }
            };

            // Add 3D Secure if enabled
            if (_settings.Enable3DSecure)
            {
                checkoutData.Add("threeDSecure.verifyAttempt", "true");
            }

            // Create request content
            var content = new FormUrlEncodedContent(checkoutData);

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            // Call HyperPay API
            var response = await _httpClient.PostAsync(
                $"{_settings.BaseUrl}/v1/checkouts",
                content
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            var hyperPayResponse = JsonSerializer.Deserialize<HyperPayCheckoutResponse>(responseContent);

            if (hyperPayResponse == null || string.IsNullOrEmpty(hyperPayResponse.Id))
            {
                _logger.LogError("HyperPay checkout creation failed: {Response}", responseContent);
                return StatusCode(500, new { message = "Failed to create payment checkout" });
            }

            // Create payment record in database with Pending status
            var payment = new Payment
            {
                BookingId = booking.Id,
                CheckoutId = hyperPayResponse.Id,
                Amount = booking.TotalAmount,
                Currency = _settings.Currency,
                Status = "Pending",
                PaymentGateway = "HyperPay",
                PaymentBrand = request.PaymentBrand ?? "CARD",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.UtcNow,
                CustomerId = booking.CustomerId,
                CustomerEmail = booking.Customer?.AppUser?.Email ?? "",
                CustomerPhone = booking.Customer?.AppUser?.PhoneNumber ?? ""
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.PaymentTimeoutMinutes);

            _logger.LogInformation(
                "Payment checkout created for booking {BookingId}, CheckoutId: {CheckoutId}",
                booking.Id,
                hyperPayResponse.Id
            );

            return Ok(new PaymentCheckoutResponseDto
            {
                CheckoutId = hyperPayResponse.Id,
                Amount = booking.TotalAmount,
                Currency = _settings.Currency,
                PaymentUrl = $"{_settings.BaseUrl}/v1/paymentWidgets.js?checkoutId={hyperPayResponse.Id}",
                ExpiresAt = expiresAt,
                TestMode = _settings.TestMode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment checkout for booking {BookingId}", request.BookingId);
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Get payment status - Called after user completes payment on HyperPay
    /// </summary>
    [HttpGet("status/{checkoutId}")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusResponseDto>> GetPaymentStatus(string checkoutId)
    {
        try
        {
            // Get payment from database
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.CheckoutId == checkoutId);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            // Query HyperPay for payment status
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var response = await _httpClient.GetAsync(
                $"{_settings.BaseUrl}/v1/checkouts/{checkoutId}/payment?entityId={_settings.EntityId}"
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            var hyperPayStatus = JsonSerializer.Deserialize<HyperPayPaymentStatusResponse>(responseContent);

            if (hyperPayStatus == null)
            {
                _logger.LogError("Failed to get payment status from HyperPay for checkout {CheckoutId}", checkoutId);
                return StatusCode(500, new { message = "Failed to retrieve payment status" });
            }

            // Update payment record based on HyperPay response
            var previousStatus = payment.Status;
            UpdatePaymentStatus(payment, hyperPayStatus);

            // Update booking status if payment was successful
            if (payment.Status == "Success" && previousStatus != "Success")
            {
                payment.Booking.Status = "Confirmed";
                payment.Booking.PaymentStatus = "Paid";
                payment.Booking.PaidAt = DateTime.UtcNow;
                payment.Booking.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Payment successful for booking {BookingId}, TransactionId: {TransactionId}",
                    payment.BookingId,
                    payment.TransactionId
                );
            }
            else if (payment.Status == "Failed" && previousStatus != "Failed")
            {
                _logger.LogWarning(
                    "Payment failed for booking {BookingId}, Reason: {Reason}",
                    payment.BookingId,
                    payment.FailureReason
                );
            }

            await _context.SaveChangesAsync();

            return Ok(new PaymentStatusResponseDto
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentBrand = payment.PaymentBrand,
                Last4Digits = payment.Last4Digits,
                TransactionId = payment.TransactionId,
                PaidAt = payment.CompletedAt,
                FailureReason = payment.FailureReason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for checkout {CheckoutId}", checkoutId);
            return StatusCode(500, new { message = "An error occurred while checking payment status" });
        }
    }

    /// <summary>
    /// Webhook endpoint for HyperPay payment notifications
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous] // Webhook is authenticated via middleware signature validation
    public async Task<IActionResult> HyperPayWebhook([FromBody] HyperPayWebhookDto webhook)
    {
        try
        {
            _logger.LogInformation(
                "Webhook received for checkout {CheckoutId}, Type: {Type}",
                webhook.Id,
                webhook.Type
            );

            // Find payment by checkout ID
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.CheckoutId == webhook.Id);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for webhook checkout {CheckoutId}", webhook.Id);
                return NotFound();
            }

            // Update payment status based on webhook data
            var previousStatus = payment.Status;
            UpdatePaymentFromWebhook(payment, webhook);

            // Update booking if payment successful
            if (payment.Status == "Success" && previousStatus != "Success")
            {
                payment.Booking.Status = "Confirmed";
                payment.Booking.PaymentStatus = "Paid";
                payment.Booking.PaidAt = DateTime.UtcNow;
                payment.Booking.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for checkout {CheckoutId}", webhook.Id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Initiate refund for a payment
    /// </summary>
    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentRefundResponseDto>> RefundPayment(
        int paymentId,
        [FromBody] PaymentRefundRequestDto request)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            if (payment.Status != "Success")
            {
                return BadRequest(new { message = "Can only refund successful payments" });
            }

            if (payment.IsRefunded)
            {
                return BadRequest(new { message = "Payment already refunded" });
            }

            // Prepare refund request to HyperPay
            var refundData = new Dictionary<string, string>
            {
                { "entityId", _settings.EntityId },
                { "amount", request.Amount.ToString("F2") },
                { "currency", payment.Currency },
                { "paymentType", "RF" } // Refund
            };

            var content = new FormUrlEncodedContent(refundData);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var response = await _httpClient.PostAsync(
                $"{_settings.BaseUrl}/v1/payments/{payment.TransactionId}",
                content
            );

            var responseContent = await response.Content.ReadAsStringAsync();
            var refundResponse = JsonSerializer.Deserialize<HyperPayRefundResponse>(responseContent);

            if (refundResponse == null || !IsSuccessCode(refundResponse.Result?.Code))
            {
                _logger.LogError("Refund failed for payment {PaymentId}: {Response}", paymentId, responseContent);
                return BadRequest(new { message = "Refund failed", details = responseContent });
            }

            // Get user ID from claims
            var userIdClaim = User.FindFirst("uid")?.Value;
            int.TryParse(userIdClaim, out int userId);

            // Create refund record
            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                RefundTransactionId = refundResponse.Id ?? "",
                RefundAmount = request.Amount,
                Reason = request.Reason,
                Status = "Completed",
                RequestedBy = userId,
                ProcessedAt = DateTime.UtcNow
            };

            _context.PaymentRefunds.Add(refund);

            // Update payment status
            if (request.Amount >= payment.Amount)
            {
                payment.IsRefunded = true;
                payment.RefundAmount = request.Amount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundReason = request.Reason;
                payment.RefundedBy = userId;
            }
            else
            {
                payment.RefundAmount += request.Amount;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Refund processed for payment {PaymentId}, Amount: {Amount}",
                paymentId,
                request.Amount
            );

            return Ok(new PaymentRefundResponseDto
            {
                RefundId = refund.Id,
                PaymentId = payment.Id,
                Amount = refund.RefundAmount,
                Status = refund.Status,
                ProcessedAt = refund.ProcessedAt ?? DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(500, new { message = "An error occurred while processing refund" });
        }
    }

    #region Helper Methods

    private void UpdatePaymentStatus(Payment payment, HyperPayPaymentStatusResponse response)
    {
        payment.TransactionId = response.Id ?? payment.TransactionId;
        payment.ResultCode = response.Result?.Code ?? "";
        payment.StatusDescription = response.Result?.Description ?? "";

        // Determine status based on result code
        if (IsSuccessCode(response.Result?.Code))
        {
            payment.Status = "Success";
            payment.CompletedAt = DateTime.UtcNow;
            payment.PaymentBrand = response.PaymentBrand ?? payment.PaymentBrand;
            payment.Last4Digits = response.Card?.Last4Digits ?? payment.Last4Digits;
            payment.CardExpiryDate = $"{response.Card?.ExpiryMonth}/{response.Card?.ExpiryYear}";
            payment.CardHolder = response.Card?.Holder ?? "";
            payment.RiskScore = response.Risk?.Score ?? "";
        }
        else if (IsPendingCode(response.Result?.Code))
        {
            payment.Status = "Pending";
        }
        else
        {
            payment.Status = "Failed";
            payment.FailedAt = DateTime.UtcNow;
            payment.FailureReason = response.Result?.Description ?? "Payment failed";
        }
    }

    private void UpdatePaymentFromWebhook(Payment payment, HyperPayWebhookDto webhook)
    {
        payment.TransactionId = webhook.ReferencedId ?? payment.TransactionId;
        payment.ResultCode = webhook.Result?.Code ?? "";
        payment.StatusDescription = webhook.Result?.Description ?? "";
        payment.WebhookPayload = JsonSerializer.Serialize(webhook);

        if (IsSuccessCode(webhook.Result?.Code))
        {
            payment.Status = "Success";
            payment.CompletedAt = DateTime.UtcNow;
        }
        else if (IsPendingCode(webhook.Result?.Code))
        {
            payment.Status = "Pending";
        }
        else
        {
            payment.Status = "Failed";
            payment.FailedAt = DateTime.UtcNow;
            payment.FailureReason = webhook.Result?.Description ?? "Payment failed";
        }
    }

    /// <summary>
    /// Check if result code indicates success
    /// HyperPay success codes: /^(000\.000\.|000\.100\.1|000\.[36])/
    /// </summary>
    private bool IsSuccessCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;

        return code.StartsWith("000.000.") ||
               code.StartsWith("000.100.1") ||
               code.StartsWith("000.3") ||
               code.StartsWith("000.6");
    }

    /// <summary>
    /// Check if result code indicates pending status
    /// </summary>
    private bool IsPendingCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;

        return code.StartsWith("000.200") || // Pending
               code.StartsWith("800.400"); // Review
    }

    #endregion
}

#region DTOs

public class HyperPayCheckoutResponse
{
    public string Id { get; set; } = string.Empty;
    public ResultDto Result { get; set; } = new();
}

public class HyperPayPaymentStatusResponse
{
    public string Id { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentBrand { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public ResultDto Result { get; set; } = new();
    public CardDto Card { get; set; } = new();
    public RiskDto Risk { get; set; } = new();
}

public class HyperPayRefundResponse
{
    public string Id { get; set; } = string.Empty;
    public ResultDto Result { get; set; } = new();
}

public class ResultDto
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CardDto
{
    public string Last4Digits { get; set; } = string.Empty;
    public string Holder { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
}

public class RiskDto
{
    public string Score { get; set; } = string.Empty;
}

#endregion
