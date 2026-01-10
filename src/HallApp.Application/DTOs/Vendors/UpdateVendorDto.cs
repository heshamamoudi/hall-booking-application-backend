#nullable enable

namespace HallApp.Application.DTOs.Vendors;

public class UpdateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty; // Frontend sends phoneNumber
    public string Website { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    
    // Business registration details
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    
    public int VendorTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    
    // Related entities - all use DTOs from Vendors namespace
    public VendorLocationDto? Location { get; set; }
    public List<VendorManagerDto>? Managers { get; set; }
    public List<ServiceItemDto>? ServiceItems { get; set; }
    public List<VendorBusinessHourDto>? BusinessHours { get; set; }
    public List<VendorBlockedDateDto>? BlockedDates { get; set; }
    
    // Flags for categorization
    public bool HasSpecialOffer { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPremium { get; set; } = false;
}
