using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace HallApp.Web.Middleware;

/// <summary>
/// Middleware to validate HyperPay webhook signatures
/// Ensures that webhook requests are authentic and come from HyperPay
/// </summary>
public class HyperPayWebhookMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HyperPayWebhookMiddleware> _logger;
    private readonly HyperPaySettings _settings;

    public HyperPayWebhookMiddleware(
        RequestDelegate next,
        ILogger<HyperPayWebhookMiddleware> logger,
        IOptions<HyperPaySettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process webhook endpoints
        if (context.Request.Path.StartsWithSegments("/api/payments/webhook"))
        {
            _logger.LogInformation("HyperPay webhook request received");

            // Read the request body
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Get signature from headers
            if (!context.Request.Headers.TryGetValue("X-HyperPay-Signature", out var signatureHeader))
            {
                _logger.LogWarning("Webhook request missing signature header");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing signature");
                return;
            }

            // Validate signature
            var expectedSignature = ComputeSignature(requestBody, _settings.WebhookSecret);
            if (!SignaturesMatch(signatureHeader.ToString(), expectedSignature))
            {
                _logger.LogWarning("Invalid webhook signature");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid signature");
                return;
            }

            _logger.LogInformation("Webhook signature validated successfully");
        }

        await _next(context);
    }

    private string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private bool SignaturesMatch(string signature1, string signature2)
    {
        if (string.IsNullOrEmpty(signature1) || string.IsNullOrEmpty(signature2))
        {
            return false;
        }

        // Use constant-time comparison to prevent timing attacks
        var bytes1 = Encoding.UTF8.GetBytes(signature1);
        var bytes2 = Encoding.UTF8.GetBytes(signature2);

        if (bytes1.Length != bytes2.Length)
        {
            return false;
        }

        int result = 0;
        for (int i = 0; i < bytes1.Length; i++)
        {
            result |= bytes1[i] ^ bytes2[i];
        }

        return result == 0;
    }
}

/// <summary>
/// HyperPay configuration settings
/// Should be stored in appsettings.json or environment variables
/// </summary>
public class HyperPaySettings
{
    public string EntityId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://eu-test.oppwa.com"; // Test environment
    public string WebhookSecret { get; set; } = string.Empty;
    public string Currency { get; set; } = "SAR";
    public bool TestMode { get; set; } = true;

    // Payment brands configuration
    public List<string> EnabledPaymentBrands { get; set; } = new()
    {
        "VISA",
        "MASTER",
        "MADA",
        "APPLEPAY",
        "STC_PAY"
    };

    // Risk management settings
    public bool Enable3DSecure { get; set; } = true;
    public int PaymentTimeoutMinutes { get; set; } = 30;
}
