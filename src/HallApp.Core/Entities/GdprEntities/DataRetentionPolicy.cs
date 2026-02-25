namespace HallApp.Core.Entities.GdprEntities;

/// <summary>
/// HIGH-012: Defines the data retention policy applied to user data.
/// Used to determine when personal data should be anonymized or deleted.
/// </summary>
public enum DataRetentionPolicy
{
    /// <summary>
    /// Default policy - data retained according to standard business rules.
    /// Personal data anonymized after the configured retention period (default: 3 years after last activity).
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Extended retention - data retained for a longer period.
    /// Typically used for accounts with active financial obligations or disputes.
    /// </summary>
    Extended = 1,

    /// <summary>
    /// Legal hold - data must not be deleted or anonymized due to ongoing legal proceedings.
    /// Overrides all other retention policies until explicitly released.
    /// </summary>
    LegalHold = 2,

    /// <summary>
    /// User has explicitly requested data deletion under GDPR Article 17 (Right to Erasure).
    /// Data will be anonymized at the next scheduled retention cycle.
    /// </summary>
    DeletionRequested = 3,

    /// <summary>
    /// Data has been fully anonymized. Personal identifiers have been replaced with hashed values.
    /// Aggregated financial data is retained for accounting compliance.
    /// </summary>
    Anonymized = 4
}
