using System.ComponentModel.DataAnnotations.Schema;

namespace HallApp.Core.Entities;

public class PlatformSettings
{
    public int Id { get; set; }

    // Company Identity
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;

    // Legal Address (ZATCA structured)
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SA";

    // Contact
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Bank
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;

    // Logo
    public string LogoUrl { get; set; } = string.Empty;

    // Default Commission Rates (decimal fraction: 0.05 = 5%)
    [Column(TypeName = "decimal(5,4)")]
    public decimal DefaultHallCommissionRate { get; set; } = 0.05m;

    [Column(TypeName = "decimal(5,4)")]
    public decimal DefaultVendorCommissionRate { get; set; } = 0.03m;

    // Metadata
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
