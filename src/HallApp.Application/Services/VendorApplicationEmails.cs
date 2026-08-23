using System.Net;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Interfaces.IServices;

namespace HallApp.Application.Services;

/// <summary>
/// The messages sent to an applicant as their registration moves along.
///
/// Kept in one place so the wording stays consistent and so every value that comes
/// from a user - business name, a reviewer's comment - is HTML-encoded exactly
/// once. A rejection comment is written by an admin and rendered in someone else's
/// mail client, so it is untrusted text like any other.
/// </summary>
public static class VendorApplicationEmails
{
    public static EmailMessage Submitted(VendorApplication application) => new()
    {
        ToAddress = application.ContactEmail,
        ToName = application.ContactPersonName,
        Subject = "We have received your Zawaji application",
        HtmlBody = Wrap(
            "Application received",
            $"<p>Thank you for applying to join Zawaji as <strong>{E(application.BusinessName)}</strong>.</p>" +
            "<p>Our team is reviewing the documents you uploaded. Each one is checked " +
            "individually, and we will email you as soon as there is a decision. If " +
            "anything needs correcting you will be told exactly which document and why.</p>")
    };

    public static EmailMessage DocumentApproved(VendorApplication application, VendorDocument document) => new()
    {
        ToAddress = application.ContactEmail,
        ToName = application.ContactPersonName,
        Subject = $"Document approved: {Humanise(document.DocumentType)}",
        HtmlBody = Wrap(
            "Document approved",
            $"<p>Your <strong>{E(Humanise(document.DocumentType))}</strong> has been approved.</p>" +
            Comment(document.ReviewComment) +
            "<p>We will let you know once the remaining documents have been reviewed.</p>")
    };

    public static EmailMessage DocumentRejected(VendorApplication application, VendorDocument document) => new()
    {
        ToAddress = application.ContactEmail,
        ToName = application.ContactPersonName,
        Subject = $"Action needed: {Humanise(document.DocumentType)}",
        HtmlBody = Wrap(
            "A document needs correcting",
            $"<p>We could not accept your <strong>{E(Humanise(document.DocumentType))}</strong>.</p>" +
            Comment(document.ReviewComment) +
            "<p>Please sign in and upload a corrected copy. Only this document needs " +
            "replacing - the others you have already sent are unaffected.</p>")
    };

    public static EmailMessage Approved(VendorApplication application) => new()
    {
        ToAddress = application.ContactEmail,
        ToName = application.ContactPersonName,
        Subject = "Your Zawaji application has been approved",
        HtmlBody = Wrap(
            "You are approved",
            $"<p>All documents for <strong>{E(application.BusinessName)}</strong> have been " +
            "approved and your account is now active.</p>" +
            "<p>You can sign in to add your services, prices, photographs and opening " +
            "hours. Your listing stays hidden from customers until you choose to " +
            "publish it, so take as long as you need to set it up.</p>")
    };

    public static EmailMessage Rejected(VendorApplication application) => new()
    {
        ToAddress = application.ContactEmail,
        ToName = application.ContactPersonName,
        Subject = "Your Zawaji application",
        HtmlBody = Wrap(
            "Application not accepted",
            $"<p>We are not able to approve the application for " +
            $"<strong>{E(application.BusinessName)}</strong> at this time.</p>" +
            Comment(application.RejectionReason) +
            "<p>If you believe this was decided in error, reply to this email and we " +
            "will take another look.</p>")
    };

    // ===================================================================

    private static string Comment(string comment) =>
        string.IsNullOrWhiteSpace(comment)
            ? string.Empty
            : $"<blockquote style=\"margin:16px 0;padding:12px 16px;border-left:3px solid #cca34c;background:#faf7f0;\">{E(comment)}</blockquote>";

    /// <summary>CommercialRegistration -> Commercial Registration.</summary>
    private static string Humanise(string documentType) =>
        string.IsNullOrWhiteSpace(documentType)
            ? documentType
            : System.Text.RegularExpressions.Regex.Replace(documentType, "(?<!^)([A-Z])", " $1");

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Wrap(string heading, string body) =>
        "<div style=\"font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;" +
        "max-width:560px;margin:0 auto;color:#1f2933;line-height:1.6;\">" +
        "<div style=\"background:#1b5e3b;padding:20px 24px;\">" +
        "<span style=\"color:#ffffff;font-size:18px;font-weight:600;\">Zawaji</span></div>" +
        $"<div style=\"padding:24px;\"><h2 style=\"margin:0 0 12px;font-size:20px;color:#1b5e3b;\">{E(heading)}</h2>{body}</div>" +
        "<div style=\"padding:16px 24px;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;\">" +
        "This is an automated message from Zawaji.</div></div>";
}
