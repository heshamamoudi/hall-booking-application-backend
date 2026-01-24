namespace HallApp.Application.Configuration;

/// <summary>
/// Root payment settings containing all provider configurations
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "Payment";

    /// <summary>
    /// Default currency for payments
    /// </summary>
    public string DefaultCurrency { get; set; } = "SAR";

    /// <summary>
    /// Enable test mode for all providers
    /// </summary>
    public bool TestMode { get; set; } = true;

    /// <summary>
    /// Payment timeout in minutes
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// HyperPay configuration
    /// </summary>
    public HyperPaySettings HyperPay { get; set; } = new();

    /// <summary>
    /// Tabby configuration
    /// </summary>
    public TabbySettings Tabby { get; set; } = new();

    /// <summary>
    /// Tamara configuration
    /// </summary>
    public TamaraSettings Tamara { get; set; } = new();
}

/// <summary>
/// HyperPay payment gateway settings
/// </summary>
public class HyperPaySettings
{
    /// <summary>
    /// Enable HyperPay payment provider
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// HyperPay Entity ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// HyperPay Access Token (Bearer token)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// API Base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://eu-test.oppwa.com";

    /// <summary>
    /// Webhook secret for signature validation
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Enabled payment brands
    /// </summary>
    public List<string> EnabledPaymentBrands { get; set; } = new()
    {
        "VISA", "MASTER", "MADA", "APPLEPAY", "STC_PAY"
    };

    /// <summary>
    /// Enable 3D Secure
    /// </summary>
    public bool Enable3DSecure { get; set; } = true;
}

/// <summary>
/// Tabby Buy Now Pay Later settings
/// https://docs.tabby.ai/
/// </summary>
public class TabbySettings
{
    /// <summary>
    /// Enable Tabby payment provider
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Tabby Public Key (for frontend)
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Tabby Secret Key (for API calls)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Merchant Code
    /// </summary>
    public string MerchantCode { get; set; } = string.Empty;

    /// <summary>
    /// API Base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.tabby.ai/api/v2";

    /// <summary>
    /// Webhook secret for signature validation
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Minimum order amount (Tabby typically requires min 100 SAR)
    /// </summary>
    public decimal MinOrderAmount { get; set; } = 100m;

    /// <summary>
    /// Maximum order amount (Tabby limit varies by merchant)
    /// </summary>
    public decimal MaxOrderAmount { get; set; } = 5000m;

    /// <summary>
    /// Available payment products
    /// </summary>
    public List<string> Products { get; set; } = new()
    {
        "installments" // Pay in 4 installments
    };
}

/// <summary>
/// Tamara Buy Now Pay Later settings
/// https://docs.tamara.co/
/// </summary>
public class TamaraSettings
{
    /// <summary>
    /// Enable Tamara payment provider
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Tamara API Token
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Merchant URL/ID
    /// </summary>
    public string MerchantUrl { get; set; } = string.Empty;

    /// <summary>
    /// Notification Token for webhooks
    /// </summary>
    public string NotificationToken { get; set; } = string.Empty;

    /// <summary>
    /// API Base URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-sandbox.tamara.co";

    /// <summary>
    /// Webhook secret for signature validation
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Minimum order amount (Tamara typically requires min 100 SAR)
    /// </summary>
    public decimal MinOrderAmount { get; set; } = 100m;

    /// <summary>
    /// Maximum order amount
    /// </summary>
    public decimal MaxOrderAmount { get; set; } = 5000m;

    /// <summary>
    /// Available payment types
    /// </summary>
    public List<string> PaymentTypes { get; set; } = new()
    {
        "PAY_BY_INSTALMENTS", // Split into 3 payments
        "PAY_BY_LATER"        // Pay in 30 days
    };

    /// <summary>
    /// Country code for Tamara
    /// </summary>
    public string CountryCode { get; set; } = "SA";
}
