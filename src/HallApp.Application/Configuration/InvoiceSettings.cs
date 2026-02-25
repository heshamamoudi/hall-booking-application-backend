namespace HallApp.Application.Configuration;

/// <summary>
/// Configuration settings for invoice generation behavior
/// </summary>
public class InvoiceSettings
{
    public const string SectionName = "Invoice";

    /// <summary>
    /// When true, invoices are automatically generated when a booking status
    /// changes to Confirmed or ReadyForPayment
    /// </summary>
    public bool AutoGenerateOnConfirmation { get; set; } = true;

    /// <summary>
    /// The booking statuses that trigger automatic invoice generation
    /// </summary>
    public List<string> AutoGenerateTriggerStatuses { get; set; } = new()
    {
        "Confirmed",
        "ReadyForPayment"
    };
}
