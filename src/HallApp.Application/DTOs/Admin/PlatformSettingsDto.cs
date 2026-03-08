using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Admin;

public class PlatformSettingsDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SA";
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public decimal DefaultHallCommissionRate { get; set; }
    public decimal DefaultVendorCommissionRate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdatePlatformSettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string BuildingNumber { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SA";
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIban { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    [Range(0, 1)]
    public decimal DefaultHallCommissionRate { get; set; }

    [Range(0, 1)]
    public decimal DefaultVendorCommissionRate { get; set; }
}
