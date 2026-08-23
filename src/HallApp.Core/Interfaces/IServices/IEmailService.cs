namespace HallApp.Core.Interfaces.IServices;

/// <summary>One outbound message.</summary>
public class EmailMessage
{
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    /// <summary>HTML body. A plain-text alternative is derived from it automatically.</summary>
    public string HtmlBody { get; set; } = string.Empty;
}

public interface IEmailService
{
    /// <summary>
    /// Sends a message. Returns false when delivery failed, but never throws: a
    /// document decision must still be recorded even if the notification bounces.
    /// With no SMTP host configured the message is logged and true is returned, so
    /// a deployment without mail credentials still works.
    /// </summary>
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
