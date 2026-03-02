using System.Security.Cryptography;
using System.Text;
using HallApp.Application.Configuration;
using Microsoft.Extensions.Options;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware to validate HyperPay webhook signatures (CRIT-SEC-001).
/// Ensures that webhook requests are authentic and come from HyperPay.
/// Only applies to the /api/payments/webhook/hyperpay endpoint.
/// Tabby and Tamara validate signatures in their own controller endpoints.
/// </summary>
public class HyperPayWebhookMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HyperPayWebhookMiddleware> _logger;
    private readonly PaymentSettings _paymentSettings;

    public HyperPayWebhookMiddleware(
        RequestDelegate next,
        ILogger<HyperPayWebhookMiddleware> logger,
        IOptions<PaymentSettings> paymentSettings)
    {
        _next = next;
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate signature on the HyperPay webhook endpoint
        if (context.Request.Path.StartsWithSegments("/api/payments/webhook/hyperpay") &&
            context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("CRIT-SEC-001: HyperPay webhook request received from {RemoteIp}",
                context.Connection.RemoteIpAddress);

            var webhookSecret = _paymentSettings.HyperPay.WebhookSecret;
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("CRIT-SEC-001: HyperPay webhook secret is not configured. " +
                    "Set Payment:HyperPay:WebhookSecret in configuration.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Webhook configuration error\"}");
                return;
            }

            // Read the request body (enable buffering so the body can be re-read by the controller)
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Get signature from headers
            if (!context.Request.Headers.TryGetValue("X-HyperPay-Signature", out var signatureHeader) ||
                string.IsNullOrWhiteSpace(signatureHeader.ToString()))
            {
                _logger.LogWarning("CRIT-SEC-001: HyperPay webhook request missing X-HyperPay-Signature header. " +
                    "RemoteIp: {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Missing webhook signature\"}");
                return;
            }

            // Validate signature using HMAC-SHA256
            var expectedSignature = ComputeHmacSha256(requestBody, webhookSecret);
            if (!ConstantTimeEquals(signatureHeader.ToString(), expectedSignature))
            {
                _logger.LogWarning("CRIT-SEC-001: HyperPay webhook signature mismatch. " +
                    "RemoteIp: {RemoteIp}. Possible forged webhook attempt.",
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Invalid webhook signature\"}");
                return;
            }

            _logger.LogInformation("CRIT-SEC-001: HyperPay webhook signature validated successfully");
        }

        await _next(context);
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks on signature validation.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);

        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
