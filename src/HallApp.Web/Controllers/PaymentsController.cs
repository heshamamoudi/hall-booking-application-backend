using HallApp.Application.Configuration;
using HallApp.Application.DTOs.Payment;
using HallApp.Application.Services.Payment;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HallApp.Web.Controllers;

/// <summary>
/// Controller for payment operations - card payments and BNPL (Buy Now Pay Later)
/// Supports HyperPay, Tabby, and Tamara payment providers
/// </summary>
public class PaymentsController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentsController> _logger;
    private readonly PaymentSettings _paymentSettings;
    private readonly IEnumerable<IPaymentProviderService> _paymentProviders;

    public PaymentsController(
        IUnitOfWork unitOfWork,
        ILogger<PaymentsController> logger,
        IOptions<PaymentSettings> paymentSettings,
        IEnumerable<IPaymentProviderService> paymentProviders)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
        _paymentProviders = paymentProviders;
    }

    /// <summary>
    /// Get available payment providers
    /// </summary>
    [HttpGet("providers")]
    public ActionResult<PaymentProvidersResponseDto> GetAvailableProviders()
    {
        var providers = _paymentProviders
            .Where(p => p.IsEnabled)
            .Select(p => new PaymentProviderDto
            {
                Id = p.Provider.ToString().ToLower(),
                Name = GetProviderDisplayName(p.Provider),
                Type = GetProviderType(p.Provider),
                Description = GetProviderDescription(p.Provider),
                MinAmount = GetProviderMinAmount(p.Provider),
                MaxAmount = GetProviderMaxAmount(p.Provider),
                SupportedBrands = GetProviderBrands(p.Provider)
            })
            .ToList();

        return Ok(new PaymentProvidersResponseDto
        {
            Providers = providers,
            DefaultCurrency = _paymentSettings.DefaultCurrency,
            TestMode = _paymentSettings.TestMode
        });
    }

    /// <summary>
    /// Initialize payment checkout - Creates a checkout session with the specified provider
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaymentCheckoutResponseDto>>> InitiateCheckout(
        [FromBody] PaymentCheckoutRequestDto request)
    {
        try
        {
            // Validate booking exists and is in correct status
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(request.BookingId);

            if (booking == null)
            {
                return Error<PaymentCheckoutResponseDto>("Booking not found", 404);
            }

            if (booking.Status == "Cancelled")
            {
                return Error<PaymentCheckoutResponseDto>("Cannot process payment for cancelled booking");
            }

            // Check if payment already exists for this booking
            var existingPayment = await _unitOfWork.PaymentRepository.GetSuccessfulPaymentByBookingIdAsync(request.BookingId);

            if (existingPayment != null)
            {
                return Error<PaymentCheckoutResponseDto>("Payment already completed for this booking");
            }

            // Get the requested payment provider
            var providerType = ParseProvider(request.Provider);
            var provider = _paymentProviders.FirstOrDefault(p => p.Provider == providerType && p.IsEnabled);

            if (provider == null)
            {
                return Error<PaymentCheckoutResponseDto>($"Payment provider '{request.Provider}' is not available");
            }

            // Build checkout request
            var checkoutRequest = BuildCheckoutRequest(booking, request);

            // Create checkout with provider
            var result = await provider.CreateCheckoutAsync(checkoutRequest);

            if (!result.Success)
            {
                _logger.LogError("Payment checkout failed for booking {BookingId} with provider {Provider}: {Error}",
                    request.BookingId, request.Provider, result.ErrorMessage);

                return Error<PaymentCheckoutResponseDto>(result.ErrorMessage ?? "Payment checkout failed");
            }

            // Create payment record in database with Pending status
            var payment = new Payment
            {
                BookingId = booking.Id,
                CheckoutId = result.CheckoutId,
                Amount = booking.TotalAmount,
                Currency = _paymentSettings.DefaultCurrency,
                Status = "Pending",
                PaymentGateway = provider.Provider.ToString(),
                PaymentBrand = request.PaymentBrand ?? "CARD",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                CreatedAt = DateTime.UtcNow,
                CustomerId = booking.CustomerId,
                CustomerEmail = booking.Customer?.AppUser?.Email ?? "",
                CustomerPhone = booking.Customer?.AppUser?.PhoneNumber ?? ""
            };

            await _unitOfWork.PaymentRepository.AddAsync(payment);
            await _unitOfWork.Complete();

            _logger.LogInformation(
                "Payment checkout created for booking {BookingId} with provider {Provider}, CheckoutId: {CheckoutId}",
                booking.Id,
                provider.Provider.ToString(),
                result.CheckoutId
            );

            return Success(new PaymentCheckoutResponseDto
            {
                CheckoutId = result.CheckoutId,
                Amount = booking.TotalAmount,
                Currency = _paymentSettings.DefaultCurrency,
                PaymentUrl = result.PaymentUrl,
                ExpiresAt = result.ExpiresAt,
                TestMode = _paymentSettings.TestMode,
                Provider = provider.Provider.ToString()
            }, "Payment checkout created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment checkout for booking {BookingId}", request.BookingId);
            return Error<PaymentCheckoutResponseDto>("An error occurred while processing your request", 500);
        }
    }

    /// <summary>
    /// Get payment status - Called after user completes payment
    /// </summary>
    [HttpGet("status/{checkoutId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaymentStatusResponseDto>>> GetPaymentStatus(string checkoutId)
    {
        try
        {
            // Get payment from database
            var payment = await _unitOfWork.PaymentRepository.GetPaymentWithBookingByCheckoutIdAsync(checkoutId);

            if (payment == null)
            {
                return Error<PaymentStatusResponseDto>("Payment not found", 404);
            }

            // Get the provider that was used for this payment
            var providerType = Enum.TryParse<PaymentProvider>(payment.PaymentGateway, out var pt) ? pt : PaymentProvider.HyperPay;
            var provider = _paymentProviders.FirstOrDefault(p => p.Provider == providerType);

            if (provider == null)
            {
                return Error<PaymentStatusResponseDto>("Payment provider not available");
            }

            // Query provider for payment status
            var statusResult = await provider.GetPaymentStatusAsync(checkoutId);

            if (!statusResult.Success && statusResult.Status != "Pending")
            {
                _logger.LogError("Failed to get payment status for checkout {CheckoutId} from {Provider}",
                    checkoutId, payment.PaymentGateway);
            }

            // Update payment record based on provider response
            var previousStatus = payment.Status;
            UpdatePaymentStatus(payment, statusResult);

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

            await _unitOfWork.Complete();

            return Success(new PaymentStatusResponseDto
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
            return Error<PaymentStatusResponseDto>("An error occurred while checking payment status", 500);
        }
    }

    /// <summary>
    /// Webhook endpoint for HyperPay payment notifications
    /// </summary>
    [HttpPost("webhook/hyperpay")]
    [AllowAnonymous]
    public async Task<IActionResult> HyperPayWebhook([FromBody] HyperPayWebhookDto webhook)
    {
        return await ProcessWebhook(PaymentProvider.HyperPay, webhook.Id ?? "");
    }

    /// <summary>
    /// Webhook endpoint for Tabby payment notifications
    /// </summary>
    [HttpPost("webhook/tabby")]
    [AllowAnonymous]
    public async Task<IActionResult> TabbyWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Tabby-Signature"].FirstOrDefault() ?? "";

        var provider = _paymentProviders.FirstOrDefault(p => p.Provider == PaymentProvider.Tabby);
        if (provider == null || !provider.ValidateWebhookSignature(payload, signature))
        {
            _logger.LogWarning("Invalid Tabby webhook signature");
            return new UnauthorizedResult();
        }

        var webhookData = provider.ParseWebhookPayload(payload);
        return await ProcessWebhook(PaymentProvider.Tabby, webhookData.CheckoutId, webhookData);
    }

    /// <summary>
    /// Webhook endpoint for Tamara payment notifications
    /// </summary>
    [HttpPost("webhook/tamara")]
    [AllowAnonymous]
    public async Task<IActionResult> TamaraWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var notificationToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "") ?? "";

        var provider = _paymentProviders.FirstOrDefault(p => p.Provider == PaymentProvider.Tamara);
        if (provider == null || !provider.ValidateWebhookSignature(payload, notificationToken))
        {
            _logger.LogWarning("Invalid Tamara webhook signature");
            return new UnauthorizedResult();
        }

        var webhookData = provider.ParseWebhookPayload(payload);

        // Tamara requires explicit authorization for approved orders
        if (webhookData.Status == "Pending" && webhookData.AdditionalData.TryGetValue("event_type", out var eventType))
        {
            if (eventType?.ToString() == "order_approved")
            {
                var tamaraProvider = provider as TamaraPaymentService;
                if (tamaraProvider != null)
                {
                    await tamaraProvider.AuthorizePaymentAsync(webhookData.CheckoutId);
                }
            }
        }

        return await ProcessWebhook(PaymentProvider.Tamara, webhookData.CheckoutId, webhookData);
    }

    /// <summary>
    /// Initiate refund for a payment
    /// </summary>
    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PaymentRefundResponseDto>>> RefundPayment(
        int paymentId,
        [FromBody] PaymentRefundRequestDto request)
    {
        try
        {
            var payment = await _unitOfWork.PaymentRepository.GetPaymentWithBookingAsync(paymentId);

            if (payment == null)
            {
                return Error<PaymentRefundResponseDto>("Payment not found", 404);
            }

            if (payment.Status != "Success")
            {
                return Error<PaymentRefundResponseDto>("Can only refund successful payments");
            }

            if (payment.IsRefunded)
            {
                return Error<PaymentRefundResponseDto>("Payment already refunded");
            }

            // Get the provider that was used for this payment
            var providerType = Enum.TryParse<PaymentProvider>(payment.PaymentGateway, out var pt) ? pt : PaymentProvider.HyperPay;
            var provider = _paymentProviders.FirstOrDefault(p => p.Provider == providerType);

            if (provider == null)
            {
                return Error<PaymentRefundResponseDto>("Payment provider not available");
            }

            // Process refund with provider
            var refundResult = await provider.ProcessRefundAsync(payment.TransactionId, request.Amount, request.Reason);

            if (!refundResult.Success)
            {
                _logger.LogError("Refund failed for payment {PaymentId}: {Error}", paymentId, refundResult.ErrorMessage);
                return Error<PaymentRefundResponseDto>(refundResult.ErrorMessage ?? "Refund failed");
            }

            // Create refund record
            var refund = new PaymentRefund
            {
                PaymentId = payment.Id,
                RefundTransactionId = refundResult.RefundId,
                RefundAmount = request.Amount,
                Reason = request.Reason,
                Status = "Completed",
                RequestedBy = UserId,
                ProcessedAt = DateTime.UtcNow
            };

            await _unitOfWork.PaymentRefundRepository.AddAsync(refund);

            // Update payment status
            if (request.Amount >= payment.Amount)
            {
                payment.IsRefunded = true;
                payment.RefundAmount = request.Amount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundReason = request.Reason;
                payment.RefundedBy = UserId;
            }
            else
            {
                payment.RefundAmount += request.Amount;
            }

            await _unitOfWork.Complete();

            _logger.LogInformation(
                "Refund processed for payment {PaymentId}, Amount: {Amount}",
                paymentId,
                request.Amount
            );

            return Success(new PaymentRefundResponseDto
            {
                RefundId = refund.Id,
                PaymentId = payment.Id,
                Amount = refund.RefundAmount,
                Status = refund.Status,
                ProcessedAt = refund.ProcessedAt ?? DateTime.UtcNow
            }, "Refund processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return Error<PaymentRefundResponseDto>("An error occurred while processing refund", 500);
        }
    }

    #region Helper Methods

    private PaymentProvider ParseProvider(string? provider)
    {
        return provider?.ToLowerInvariant() switch
        {
            "hyperpay" => PaymentProvider.HyperPay,
            "tabby" => PaymentProvider.Tabby,
            "tamara" => PaymentProvider.Tamara,
            _ => PaymentProvider.HyperPay // Default to HyperPay
        };
    }

    private PaymentCheckoutRequest BuildCheckoutRequest(
        Core.Entities.BookingEntities.Booking booking,
        PaymentCheckoutRequestDto request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var checkoutRequest = new PaymentCheckoutRequest
        {
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            Amount = booking.TotalAmount,
            Currency = _paymentSettings.DefaultCurrency,
            CustomerEmail = booking.Customer?.AppUser?.Email ?? "",
            CustomerPhone = booking.Customer?.AppUser?.PhoneNumber ?? "",
            CustomerFirstName = booking.Customer?.AppUser?.FirstName ?? "",
            CustomerLastName = booking.Customer?.AppUser?.LastName ?? "",
            Description = $"Booking #{booking.Id} - {booking.Hall?.Name ?? "Hall Booking"}",
            ReturnUrl = request.ReturnUrl ?? $"{baseUrl}/payment/success",
            CancelUrl = request.CancelUrl ?? $"{baseUrl}/payment/cancel",
            WebhookUrl = $"{baseUrl}/api/payments/webhook/{request.Provider?.ToLower() ?? "hyperpay"}",
            BillingAddress = new PaymentBillingAddress
            {
                City = "Riyadh",
                Address = "Saudi Arabia",
                Country = "SA"
            }
        };

        // Build line items for BNPL providers
        var lineItems = new List<PaymentLineItem>();

        // Hall booking as a line item
        lineItems.Add(new PaymentLineItem
        {
            Name = $"Hall Booking - {booking.Hall?.Name ?? "Hall"}",
            Description = $"Event Date: {booking.EventDate:yyyy-MM-dd}",
            Quantity = 1,
            UnitPrice = booking.HallCost,
            TotalPrice = booking.HallCost,
            Category = "Event Services",
            Sku = $"HALL-{booking.HallId}"
        });

        // Add vendor services
        if (booking.VendorBookings != null)
        {
            foreach (var vendorBooking in booking.VendorBookings)
            {
                if (vendorBooking.Services != null)
                {
                    foreach (var service in vendorBooking.Services)
                    {
                        lineItems.Add(new PaymentLineItem
                        {
                            Name = service.ServiceItem?.Name ?? "Service",
                            Description = service.ServiceItem?.Description ?? "",
                            Quantity = service.Quantity,
                            UnitPrice = service.UnitPrice,
                            TotalPrice = service.TotalPrice,
                            Category = "Vendor Services",
                            Sku = $"SVC-{service.ServiceItemId}"
                        });
                    }
                }
            }
        }

        checkoutRequest.LineItems = lineItems;

        return checkoutRequest;
    }

    private void UpdatePaymentStatus(Payment payment, PaymentStatusResult statusResult)
    {
        payment.TransactionId = string.IsNullOrEmpty(statusResult.TransactionId) ? payment.TransactionId : statusResult.TransactionId;
        payment.ResultCode = statusResult.ErrorCode;
        payment.StatusDescription = statusResult.ErrorMessage;

        if (statusResult.Success || statusResult.Status == "Success")
        {
            payment.Status = "Success";
            payment.CompletedAt = DateTime.UtcNow;
            payment.PaymentBrand = string.IsNullOrEmpty(statusResult.PaymentMethod) ? payment.PaymentBrand : statusResult.PaymentMethod;
            payment.Last4Digits = string.IsNullOrEmpty(statusResult.CardLast4) ? payment.Last4Digits : statusResult.CardLast4;
            if (!string.IsNullOrEmpty(statusResult.CardExpiry))
            {
                payment.CardExpiryDate = statusResult.CardExpiry;
            }
        }
        else if (statusResult.Status == "Pending")
        {
            payment.Status = "Pending";
        }
        else
        {
            payment.Status = "Failed";
            payment.FailedAt = DateTime.UtcNow;
            payment.FailureReason = statusResult.ErrorMessage ?? "Payment failed";
        }
    }

    private async Task<IActionResult> ProcessWebhook(PaymentProvider providerType, string checkoutId, PaymentWebhookData? webhookData = null)
    {
        try
        {
            _logger.LogInformation("Webhook received for {Provider}, CheckoutId: {CheckoutId}", providerType, checkoutId);

            var payment = await _unitOfWork.PaymentRepository.GetPaymentWithBookingByCheckoutIdAsync(checkoutId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for webhook checkout {CheckoutId}", checkoutId);
                return new NotFoundResult();
            }

            var previousStatus = payment.Status;

            if (webhookData != null)
            {
                // Use parsed webhook data
                payment.TransactionId = string.IsNullOrEmpty(webhookData.TransactionId) ? payment.TransactionId : webhookData.TransactionId;

                switch (webhookData.Status)
                {
                    case "Success":
                        payment.Status = "Success";
                        payment.CompletedAt = DateTime.UtcNow;
                        break;
                    case "Pending":
                        payment.Status = "Pending";
                        break;
                    default:
                        payment.Status = "Failed";
                        payment.FailedAt = DateTime.UtcNow;
                        payment.FailureReason = "Payment failed via webhook";
                        break;
                }
            }
            else
            {
                // Query provider for status
                var provider = _paymentProviders.FirstOrDefault(p => p.Provider == providerType);
                if (provider != null)
                {
                    var statusResult = await provider.GetPaymentStatusAsync(checkoutId);
                    UpdatePaymentStatus(payment, statusResult);
                }
            }

            // Update booking if payment successful
            if (payment.Status == "Success" && previousStatus != "Success")
            {
                payment.Booking.Status = "Confirmed";
                payment.Booking.PaymentStatus = "Paid";
                payment.Booking.PaidAt = DateTime.UtcNow;
                payment.Booking.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.Complete();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for {Provider}, CheckoutId: {CheckoutId}", providerType, checkoutId);
            return StatusCode(500);
        }
    }

    private string GetProviderDisplayName(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => "HyperPay",
            PaymentProvider.Tabby => "Tabby - Pay in 4",
            PaymentProvider.Tamara => "Tamara - Buy Now Pay Later",
            _ => provider.ToString()
        };
    }

    private string GetProviderType(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => "card",
            PaymentProvider.Tabby => "bnpl",
            PaymentProvider.Tamara => "bnpl",
            _ => "card"
        };
    }

    private string GetProviderDescription(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => "Pay with credit/debit card (VISA, Mastercard, mada, Apple Pay, STC Pay)",
            PaymentProvider.Tabby => "Split your purchase into 4 interest-free payments",
            PaymentProvider.Tamara => "Pay in 30 days or split into 3 monthly payments",
            _ => ""
        };
    }

    private decimal GetProviderMinAmount(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => 0,
            PaymentProvider.Tabby => _paymentSettings.Tabby.MinOrderAmount,
            PaymentProvider.Tamara => _paymentSettings.Tamara.MinOrderAmount,
            _ => 0
        };
    }

    private decimal GetProviderMaxAmount(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => decimal.MaxValue,
            PaymentProvider.Tabby => _paymentSettings.Tabby.MaxOrderAmount,
            PaymentProvider.Tamara => _paymentSettings.Tamara.MaxOrderAmount,
            _ => decimal.MaxValue
        };
    }

    private List<string> GetProviderBrands(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.HyperPay => _paymentSettings.HyperPay.EnabledPaymentBrands,
            PaymentProvider.Tabby => new List<string> { "installments" },
            PaymentProvider.Tamara => _paymentSettings.Tamara.PaymentTypes,
            _ => new List<string>()
        };
    }

    #endregion
}

#region DTOs

public class PaymentProvidersResponseDto
{
    public List<PaymentProviderDto> Providers { get; set; } = new();
    public string DefaultCurrency { get; set; } = "SAR";
    public bool TestMode { get; set; }
}

public class PaymentProviderDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public List<string> SupportedBrands { get; set; } = new();
}

#endregion
