using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Entities;

/// <summary>
/// Represents an organization that groups halls or vendors under a single management entity.
/// Created by a HallOrganizationManager or VendorOrganizationManager to manage their team.
/// Contains consolidated business registration, contact, address, and banking information
/// used for invoicing (ZATCA compliance) and organization profile display.
/// </summary>
public class Organization
{
    public int Id { get; set; }

    /// <summary>
    /// Display name of the organization (e.g., "Al-Faisal Events Group").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization type: 'HallManagement' or 'VendorManagement'.
    /// Determines whether this organization manages halls or vendors.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The AppUser who owns this organization (HallOrganizationManager or VendorOrganizationManager).
    /// </summary>
    public int OwnerId { get; set; }
    public AppUser Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // ---------------------------------------------------------------
    // Business Registration Fields
    // ---------------------------------------------------------------

    /// <summary>
    /// Commercial Registration number (Saudi CR). 10 digits.
    /// </summary>
    public string CommercialRegistrationNumber { get; set; } = string.Empty;

    /// <summary>
    /// VAT registration number (Saudi ZATCA). 15 digits, starts and ends with '3'.
    /// </summary>
    public string VatNumber { get; set; } = string.Empty;

    /// <summary>
    /// Official legal name of the business as registered with the government.
    /// </summary>
    public string LegalName { get; set; } = string.Empty;

    // ---------------------------------------------------------------
    // Legal Address (structured fields for ZATCA compliance)
    // ---------------------------------------------------------------

    /// <summary>
    /// Building number portion of the legal address.
    /// </summary>
    public string BuildingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Street name portion of the legal address.
    /// </summary>
    public string StreetName { get; set; } = string.Empty;

    /// <summary>
    /// District (neighborhood/hay) portion of the legal address.
    /// </summary>
    public string District { get; set; } = string.Empty;

    /// <summary>
    /// City portion of the legal address.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Postal code portion of the legal address.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (defaults to "SA" for Saudi Arabia).
    /// </summary>
    public string CountryCode { get; set; } = "SA";

    // ---------------------------------------------------------------
    // Contact Information
    // ---------------------------------------------------------------

    /// <summary>
    /// Primary contact email for the organization.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact phone number for the organization.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp contact number for the organization.
    /// </summary>
    public string WhatsApp { get; set; } = string.Empty;

    /// <summary>
    /// Organization website URL.
    /// </summary>
    public string Website { get; set; } = string.Empty;

    // ---------------------------------------------------------------
    // Bank Account Information
    // ---------------------------------------------------------------

    /// <summary>
    /// Name of the bank where the organization holds its account.
    /// </summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number.
    /// </summary>
    public string BankAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// International Bank Account Number (IBAN). Saudi format: SA followed by 22 alphanumeric characters.
    /// </summary>
    public string BankIban { get; set; } = string.Empty;

    // ---------------------------------------------------------------
    // Metadata
    // ---------------------------------------------------------------

    /// <summary>
    /// Last time business info fields were updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Whether this organization's business registration has been verified by an admin.
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Timestamp of when the organization was verified.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    // Navigation properties
    public List<OrganizationMember> Members { get; set; } = new();
    public List<Hall> Halls { get; set; } = new();
    public List<Vendor> Vendors { get; set; } = new();
}
