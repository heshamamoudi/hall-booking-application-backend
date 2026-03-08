using HallApp.Application.Configuration;
using HallApp.Application.DTOs.Payment;
using HallApp.Application.Services.Payment;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using HallApp.Web.Filters;
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
    private readonly IInvoiceService _invoiceService;
    private readonly IFinancialAuditService _auditService;
    private readonly INotificationService _notificationService;

    public PaymentsController(
        IUnitOfWork unitOfWork,
        ILogger<PaymentsController> logger,
        IOptions<PaymentSettings> paymentSettings,
        IEnumerable<IPaymentProviderService> paymentProviders,
        IInvoiceService invoiceService,
        IFinancialAuditService auditService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
        _paymentProviders = paymentProviders;
        _invoiceService = invoiceService;
        _auditService = auditService;
        _notificationService = notificationService;
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
    /// Requires X-Idempotency-Key header to prevent duplicate payment sessions
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    [Idempotency("payment_checkout", required: true)]
    public async Task<ActionResult<ApiResponse<PaymentCheckoutResponseDto>>> InitiateCheckout(
        [FromBody] PaymentCheckoutRequestDto request)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            // Validate booking exists and is in correct status
            var booking = await _unitOfWork.BookingRepository.GetBookingWithDetailsAsync(request.BookingId);

            if (booking == null)
            {
                return Error<PaymentCheckoutResponseDto>("Booking not found", 404);
            }

            // RBAC-002: Verify booking ownership - only the booking's customer or admin can initiate payment
            if (!IsAdmin)
            {
                var customer = await _unitOfWork.CustomerRepository.GetCustomerByAppUserIdAsync(UserId);
                if (customer == null || booking.CustomerId != customer.Id)
                {
                    _logger.LogWarning(
                        "RBAC-002: User {UserId} attempted to initiate checkout for booking {BookingId} they do not own. " +
                        "CorrelationId: {CorrelationId}",
                        UserId, request.BookingId, correlationId);
                    return Error<PaymentCheckoutResponseDto>("You can only initiate payment for your own bookings", 403);
                }
            }

            if (booking.Status == "Cancelled")
            {
                _logger.LogWarning(
                    "HIGH-011: Payment checkout rejected - booking cancelled. " +
                    "BookingId: {BookingId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                    request.BookingId, UserId, correlationId);
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
                _logger.LogError(
                    "HIGH-011: Payment checkout failed. BookingId: {BookingId}, Provider: {Provider}, " +
                    "ErrorCode: {ErrorCode}, Error: {Error}, CorrelationId: {CorrelationId}",
                    request.BookingId, request.Provider, result.ErrorCode, result.ErrorMessage, correlationId);

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

            // HIGH-011: Enhanced structured logging for checkout creation
            _logger.LogInformation(
                "HIGH-011: Payment checkout created. UserId: {UserId}, BookingId: {BookingId}, " +
                "Amount: {Amount} {Currency}, Provider: {Provider}, CheckoutId: {CheckoutId}, " +
                "CorrelationId: {CorrelationId}",
                UserId, booking.Id, booking.TotalAmount, _paymentSettings.DefaultCurrency,
                provider.Provider.ToString(), result.CheckoutId, correlationId);

            // HIGH-006: Audit trail for checkout creation
            await _auditService.LogAsync(
                UserId, FinancialOperationType.PaymentCheckoutCreated,
                "Payment", payment.Id.ToString(),
                afterState: new { payment.BookingId, payment.Amount, payment.Currency, payment.CheckoutId, payment.PaymentGateway },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.FirstOrDefault() ?? "",
                correlationId: correlationId,
                description: $"Checkout created for booking {booking.Id} via {provider.Provider}");

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
            _logger.LogError(ex,
                "HIGH-011: Error initiating payment checkout. BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                request.BookingId, correlationId);
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
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            // Get payment from database
            var payment = await _unitOfWork.PaymentRepository.GetPaymentWithBookingByCheckoutIdAsync(checkoutId);

            if (payment == null)
            {
                return Error<PaymentStatusResponseDto>("Payment not found", 404);
            }

            // CRIT-SEC-004: Verify booking ownership - only the booking's customer or admin can view payment status
            if (!IsAdmin)
            {
                var customer = await _unitOfWork.CustomerRepository.GetCustomerByAppUserIdAsync(UserId);
                if (customer == null || payment.Booking?.CustomerId != customer.Id)
                {
                    _logger.LogWarning(
                        "CRIT-SEC-004: User {UserId} attempted to view payment status for checkout {CheckoutId} " +
                        "belonging to booking {BookingId} they do not own. CorrelationId: {CorrelationId}",
                        UserId, checkoutId, payment.BookingId, correlationId);
                    return Error<PaymentStatusResponseDto>("You can only view payment status for your own bookings", 403);
                }
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
                _logger.LogError(
                    "HIGH-011: Failed to get payment status from provider. CheckoutId: {CheckoutId}, " +
                    "Provider: {Provider}, CorrelationId: {CorrelationId}",
                    checkoutId, payment.PaymentGateway, correlationId);
            }

            // Update payment record based on provider response
            var previousStatus = payment.Status;
            UpdatePaymentStatus(payment, statusResult);

            // Update booking status if payment was successful
            if (payment.Status == "Success" && previousStatus != "Success" && payment.Booking != null)
            {
                payment.Booking.Status = "Confirmed";
                payment.Booking.PaymentStatus = "Paid";
                payment.Booking.PaymentMethod = payment.PaymentBrand;
                payment.Booking.PaidAt = DateTime.UtcNow;
                payment.Booking.UpdatedAt = DateTime.UtcNow;

                // HIGH-011: Enhanced structured logging for payment success
                _logger.LogInformation(
                    "HIGH-011: Payment successful. PaymentId: {PaymentId}, BookingId: {BookingId}, " +
                    "PreviousStatus: {PreviousStatus}, NewStatus: Success, " +
                    "TransactionId: {TransactionId}, Amount: {Amount} {Currency}, " +
                    "CorrelationId: {CorrelationId}",
                    payment.Id, payment.BookingId, previousStatus,
                    payment.TransactionId, payment.Amount, payment.Currency, correlationId);

                // HIGH-006: Audit trail for payment status change
                await _auditService.LogAsync(
                    null, FinancialOperationType.PaymentStatusChanged,
                    "Payment", payment.Id.ToString(),
                    beforeState: new { Status = previousStatus },
                    afterState: new { payment.Status, payment.TransactionId, payment.CompletedAt },
                    correlationId: correlationId,
                    description: $"Payment succeeded for booking {payment.BookingId}");

                // Auto-generate invoice for the paid booking
                try
                {
                    await _invoiceService.GenerateInvoiceForBookingAsync(
                        payment.BookingId, "system-payment");

                    _logger.LogInformation(
                        "HIGH-011: Invoice auto-generated. BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                        payment.BookingId, correlationId);
                }
                catch (Exception invoiceEx)
                {
                    // HIGH-007: If invoice generation fails, log but do not revert payment
                    _logger.LogError(invoiceEx,
                        "HIGH-007: Failed to auto-generate invoice for booking {BookingId}. " +
                        "Payment remains successful. Manual invoice generation required. " +
                        "CorrelationId: {CorrelationId}",
                        payment.BookingId, correlationId);
                }
            }
            else if (payment.Status == "Failed" && previousStatus != "Failed")
            {
                // HIGH-011: Enhanced logging for payment failure
                _logger.LogWarning(
                    "HIGH-011: Payment failed. PaymentId: {PaymentId}, BookingId: {BookingId}, " +
                    "PreviousStatus: {PreviousStatus}, FailureReason: {Reason}, " +
                    "CorrelationId: {CorrelationId}",
                    payment.Id, payment.BookingId, previousStatus,
                    payment.FailureReason, correlationId);

                // HIGH-007: Compensation logic - revert booking status on payment failure
                if (payment.Booking != null && (payment.Booking.Status == "Confirmed" || payment.Booking.PaymentStatus == "Paid"))
                {
                    var bookingBeforeState = new
                    {
                        payment.Booking.Status,
                        payment.Booking.PaymentStatus,
                        payment.Booking.PaidAt
                    };

                    payment.Booking.Status = "ReadyForPayment";
                    payment.Booking.PaymentStatus = "Pending";
                    payment.Booking.PaidAt = null;
                    payment.Booking.UpdatedAt = DateTime.UtcNow;

                    _logger.LogWarning(
                        "HIGH-007: Booking status reverted due to payment failure. " +
                        "BookingId: {BookingId}, From: {FromStatus} to ReadyForPayment, " +
                        "CorrelationId: {CorrelationId}",
                        payment.BookingId, bookingBeforeState.Status, correlationId);

                    // HIGH-006: Audit trail for compensation
                    await _auditService.LogAsync(
                        null, FinancialOperationType.BookingStatusReverted,
                        "Booking", payment.BookingId.ToString(),
                        beforeState: bookingBeforeState,
                        afterState: new { Status = "ReadyForPayment", PaymentStatus = "Pending", PaidAt = (DateTime?)null },
                        correlationId: correlationId,
                        description: $"Booking reverted from {bookingBeforeState.Status} to ReadyForPayment due to payment failure");

                    // HIGH-007: Notify customer about failed payment
                    try
                    {
                        if (payment.Booking.Customer?.AppUser != null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                payment.Booking.Customer.AppUser.Id,
                                "Payment Failed",
                                $"Your payment for booking #{payment.BookingId} was not successful. Please try again.",
                                "Payment");
                        }
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogError(notifyEx,
                            "HIGH-007: Failed to send payment failure notification for booking {BookingId}",
                            payment.BookingId);
                    }
                }

                // HIGH-006: Audit trail for payment failure
                await _auditService.LogAsync(
                    null, FinancialOperationType.PaymentCompensation,
                    "Payment", payment.Id.ToString(),
                    beforeState: new { Status = previousStatus },
                    afterState: new { payment.Status, payment.FailureReason },
                    correlationId: correlationId,
                    description: $"Payment failed for booking {payment.BookingId}: {payment.FailureReason}");
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
            _logger.LogError(ex,
                "HIGH-011: Error getting payment status. CheckoutId: {CheckoutId}, CorrelationId: {CorrelationId}",
                checkoutId, correlationId);
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
    /// Requires X-Idempotency-Key header to prevent duplicate refund processing
    /// </summary>
    [HttpPost("{paymentId}/refund")]
    [Authorize(Roles = "Admin")]
    [Idempotency("payment_refund", required: true)]
    public async Task<ActionResult<ApiResponse<PaymentRefundResponseDto>>> RefundPayment(
        int paymentId,
        [FromBody] PaymentRefundRequestDto request)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];

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

            // Capture before state for audit
            var beforeState = new
            {
                payment.Status, payment.IsRefunded, payment.RefundAmount, payment.RefundedAt
            };

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
                _logger.LogError(
                    "HIGH-011: Refund failed. PaymentId: {PaymentId}, Amount: {Amount}, " +
                    "Provider: {Provider}, Error: {Error}, CorrelationId: {CorrelationId}",
                    paymentId, request.Amount, payment.PaymentGateway, refundResult.ErrorMessage, correlationId);
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

            // HIGH-011: Enhanced structured logging for refund
            _logger.LogInformation(
                "HIGH-011: Refund processed. PaymentId: {PaymentId}, RefundAmount: {RefundAmount}, " +
                "Reason: {Reason}, RefundTransactionId: {RefundTransactionId}, " +
                "IsFullRefund: {IsFullRefund}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                paymentId, request.Amount, request.Reason, refundResult.RefundId,
                request.Amount >= payment.Amount, UserId, correlationId);

            // HIGH-006: Audit trail for refund
            await _auditService.LogAsync(
                UserId, FinancialOperationType.PaymentRefundProcessed,
                "Payment", payment.Id.ToString(),
                beforeState: beforeState,
                afterState: new { payment.IsRefunded, payment.RefundAmount, payment.RefundedAt, RefundTransactionId = refundResult.RefundId },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                userAgent: Request.Headers.UserAgent.FirstOrDefault() ?? "",
                correlationId: correlationId,
                description: $"Refund of {request.Amount} {payment.Currency} for payment {paymentId}. Reason: {request.Reason}");

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
            _logger.LogError(ex,
                "HIGH-011: Error processing refund. PaymentId: {PaymentId}, CorrelationId: {CorrelationId}",
                paymentId, correlationId);
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
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            _logger.LogInformation(
                "HIGH-011: Webhook received. Provider: {Provider}, CheckoutId: {CheckoutId}, CorrelationId: {CorrelationId}",
                providerType, checkoutId, correlationId);

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

            // HIGH-011: Log status change
            if (payment.Status != previousStatus)
            {
                _logger.LogInformation(
                    "HIGH-011: Payment status changed via webhook. PaymentId: {PaymentId}, " +
                    "BookingId: {BookingId}, PreviousStatus: {PreviousStatus}, NewStatus: {NewStatus}, " +
                    "CorrelationId: {CorrelationId}",
                    payment.Id, payment.BookingId, previousStatus, payment.Status, correlationId);
            }

            // Update booking if payment successful
            if (payment.Status == "Success" && previousStatus != "Success" && payment.Booking != null)
            {
                payment.Booking.Status = "Confirmed";
                payment.Booking.PaymentStatus = "Paid";
                payment.Booking.PaymentMethod = payment.PaymentBrand;
                payment.Booking.PaidAt = DateTime.UtcNow;
                payment.Booking.UpdatedAt = DateTime.UtcNow;
            }
            // HIGH-007: Compensation on webhook failure
            else if (payment.Status == "Failed" && previousStatus != "Failed")
            {
                if (payment.Booking != null && (payment.Booking.Status == "Confirmed" || payment.Booking.PaymentStatus == "Paid"))
                {
                    payment.Booking.Status = "ReadyForPayment";
                    payment.Booking.PaymentStatus = "Pending";
                    payment.Booking.PaidAt = null;
                    payment.Booking.UpdatedAt = DateTime.UtcNow;

                    _logger.LogWarning(
                        "HIGH-007: Booking status reverted via webhook. BookingId: {BookingId}, " +
                        "CorrelationId: {CorrelationId}",
                        payment.BookingId, correlationId);
                }
            }

            await _unitOfWork.Complete();

            // Auto-generate invoice after successful payment (via webhook)
            if (payment.Status == "Success" && previousStatus != "Success")
            {
                try
                {
                    await _invoiceService.GenerateInvoiceForBookingAsync(
                        payment.BookingId, "system-webhook");
                    _logger.LogInformation(
                        "HIGH-011: Invoice auto-generated via webhook. BookingId: {BookingId}, CorrelationId: {CorrelationId}",
                        payment.BookingId, correlationId);
                }
                catch (Exception invoiceEx)
                {
                    _logger.LogError(invoiceEx,
                        "HIGH-007: Failed to auto-generate invoice via webhook for booking {BookingId}. CorrelationId: {CorrelationId}",
                        payment.BookingId, correlationId);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-011: Error processing webhook. Provider: {Provider}, CheckoutId: {CheckoutId}, CorrelationId: {CorrelationId}",
                providerType, checkoutId, correlationId);
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
