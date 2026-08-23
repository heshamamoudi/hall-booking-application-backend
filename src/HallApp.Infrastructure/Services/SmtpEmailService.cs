using System.Text.RegularExpressions;
using HallApp.Application.Configuration;
using HallApp.Core.Interfaces.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using MimeKit;
using MimeKit.Text;

namespace HallApp.Infrastructure.Services;

/// <summary>
/// SMTP delivery via MailKit, with both authentication modes Microsoft 365 needs.
///
/// Basic auth (host, username, password) works with any provider. Microsoft has
/// been switching SMTP AUTH off by default for new tenants, so OAuth2 is also
/// supported: a client-credentials token from Entra ID presented over SASL
/// XOAUTH2. Set Email:AuthMode to OAuth2 and supply TenantId, ClientId and
/// ClientSecret; the app registration needs the SMTP.SendAsApp application
/// permission with admin consent.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private const string ExchangeScope = "https://outlook.office365.com/.default";

    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.ToAddress))
        {
            _logger.LogWarning("Refusing to send an email with no recipient. Subject: {Subject}", message.Subject);
            return false;
        }

        // No mail configured is a normal state, not a failure. Log the intent so the
        // flow is still traceable in an environment without credentials.
        if (!_settings.IsConfigured)
        {
            _logger.LogInformation(
                "Email not configured (Email__Host unset). Would have sent \"{Subject}\" to {Recipient}",
                message.Subject, message.ToAddress);
            return true;
        }

        try
        {
            var mime = BuildMessage(message);

            using var client = new SmtpClient();

            var socketOptions = _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            await AuthenticateAsync(client, cancellationToken);

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Sent \"{Subject}\" to {Recipient}", message.Subject, message.ToAddress);
            return true;
        }
        catch (Exception ex)
        {
            // Never rethrow. The caller is recording a business decision; a bounced
            // notification must not roll that back.
            _logger.LogError(ex,
                "Failed to send \"{Subject}\" to {Recipient}", message.Subject, message.ToAddress);
            return false;
        }
    }

    private async Task AuthenticateAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        if (_settings.UsesOAuth)
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(_settings.ClientId)
                .WithClientSecret(_settings.ClientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{_settings.TenantId}/v2.0")
                .Build();

            var token = await app
                .AcquireTokenForClient(new[] { ExchangeScope })
                .ExecuteAsync(cancellationToken);

            var mailbox = string.IsNullOrWhiteSpace(_settings.OAuthUserName)
                ? _settings.FromAddress
                : _settings.OAuthUserName;

            await client.AuthenticateAsync(
                new SaslMechanismOAuth2(mailbox, token.AccessToken), cancellationToken);
            return;
        }

        // Some relays accept unauthenticated submission from trusted networks.
        if (string.IsNullOrWhiteSpace(_settings.UserName))
        {
            return;
        }

        await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
    }

    private MimeMessage BuildMessage(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.ToAddress));

        if (!string.IsNullOrWhiteSpace(_settings.ReplyToAddress))
        {
            mime.ReplyTo.Add(new MailboxAddress(_settings.FromName, _settings.ReplyToAddress));
        }

        mime.Subject = message.Subject;

        // Both parts, so the message is readable in clients that refuse HTML and
        // is less likely to be scored as spam.
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = ToPlainText(message.HtmlBody)
        }.ToMessageBody();

        return mime;
    }

    /// <summary>
    /// Rough HTML-to-text for the alternative part. Block tags become newlines so
    /// the result reads as paragraphs rather than one run-on line.
    /// </summary>
    private static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = Regex.Replace(html, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<\s*/\s*(p|div|tr|h[1-6]|li)\s*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<.*?>", string.Empty, RegexOptions.Singleline);
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");

        return text.Trim();
    }
}
