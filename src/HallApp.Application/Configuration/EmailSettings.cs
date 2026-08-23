namespace HallApp.Application.Configuration;

/// <summary>
/// How outbound mail is sent. Everything is optional: with no Host configured the
/// email service logs what it would have sent and returns success, so a deployment
/// without mail credentials still works rather than failing every registration.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>SMTP server. Microsoft 365 is smtp.office365.com.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>587 for STARTTLS (Microsoft 365, Gmail), 465 for implicit TLS.</summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// STARTTLS on 587. Microsoft 365 requires this and refuses implicit TLS.
    /// Set false only for a port-465 provider.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>Address recipients see. Microsoft 365 requires this to be a real mailbox.</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Zawaji";

    /// <summary>Where replies go, when different from FromAddress.</summary>
    public string ReplyToAddress { get; set; } = string.Empty;

    /// <summary>
    /// "Basic" for username and password, "OAuth2" for Microsoft 365 tenants with
    /// SMTP basic authentication disabled - which is the default for new tenants.
    /// </summary>
    public string AuthMode { get; set; } = "Basic";

    /// <summary>Basic auth. On Microsoft 365 this is the mailbox address.</summary>
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // --- OAuth2 (Microsoft 365 / Entra ID) -------------------------------------
    // Register an app, grant it the SMTP.SendAsApp application permission, and
    // grant admin consent. The token is fetched with the client credentials flow
    // and presented over SASL XOAUTH2.

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Mailbox to authenticate as under OAuth2. Defaults to FromAddress.
    /// </summary>
    public string OAuthUserName { get; set; } = string.Empty;

    /// <summary>
    /// Send mail on a background thread instead of making the caller wait. Delivery
    /// failures are logged, never surfaced to the user mid-request.
    /// </summary>
    public bool SendInBackground { get; set; } = true;

    /// <summary>Base URL used to build links in emails, e.g. https://zawajeapp.com</summary>
    public string AppBaseUrl { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host)
                                && !string.IsNullOrWhiteSpace(FromAddress);

    public bool UsesOAuth => AuthMode.Equals("OAuth2", StringComparison.OrdinalIgnoreCase);
}
