#nullable enable
using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class UpdateVendorDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Invalid phone format")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Url(ErrorMessage = "Invalid website URL")]
    public string Website { get; set; } = string.Empty;
    
    public string WhatsApp { get; set; } = string.Empty;
    
    [Url(ErrorMessage = "Invalid logo URL")]
    public string LogoUrl { get; set; } = string.Empty;
    
    [Url(ErrorMessage = "Invalid cover image URL")]
    public string CoverImageUrl { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "Commercial registration number cannot exceed 50 characters")]
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    
    [StringLength(50, ErrorMessage = "VAT number cannot exceed 50 characters")]
    public string VatNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vendor type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Valid vendor type is required")]
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
