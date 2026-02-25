namespace HallApp.Application.Configuration;

/// <summary>
/// Business rules configuration settings
/// These values can be customized per environment via appsettings.json
/// </summary>
public class BusinessRulesSettings
{
    /// <summary>
    /// Tax rate as a decimal (e.g., 0.15 for 15% VAT in Saudi Arabia)
    /// Default: 0.15 (15% Saudi VAT)
    /// </summary>
    public decimal TaxRate { get; set; } = 0.15m;

    /// <summary>
    /// Maximum booking window in days from today
    /// Default: 365 days (1 year)
    /// </summary>
    public int MaxBookingWindowDays { get; set; } = 365;

    /// <summary>
    /// Hall opening time (24-hour format)
    /// Default: 08:00
    /// </summary>
    public TimeSpan HallOpeningTime { get; set; } = new TimeSpan(8, 0, 0);

    /// <summary>
    /// Hall closing time (24-hour format)
    /// Default: 23:00
    /// </summary>
    public TimeSpan HallClosingTime { get; set; } = new TimeSpan(23, 0, 0);

    /// <summary>
    /// Minimum booking duration in hours
    /// Default: 2 hours
    /// </summary>
    public int MinBookingDurationHours { get; set; } = 2;

    /// <summary>
    /// Maximum booking duration in hours
    /// Default: 16 hours
    /// </summary>
    public int MaxBookingDurationHours { get; set; } = 16;

    /// <summary>
    /// Invoice payment due days from issue date
    /// Default: 30 days
    /// </summary>
    public int InvoicePaymentDueDays { get; set; } = 30;

    /// <summary>
    /// Idempotency key expiration in hours
    /// Default: 24 hours
    /// </summary>
    public int IdempotencyKeyExpirationHours { get; set; } = 24;

    /// <summary>
    /// Maximum refund days after payment
    /// Default: 14 days
    /// </summary>
    public int MaxRefundDays { get; set; } = 14;
}
