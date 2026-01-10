using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string LogoUrl { get; set; }
    public string CoverImageUrl { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Website { get; set; }
    
    // Business registration details
    public string CommercialRegistrationNumber { get; set; }
    public string VatNumber { get; set; }
    
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int VendorManagerId { get; set; }
    public int VendorTypeId { get; set; }
    public VendorTypeDto VendorType { get; set; }
    public List<ServiceItemDto> ServiceItems { get; set; }
    public VendorLocationDto Location { get; set; }
    public List<VendorLocationDto> Locations { get; set; }
    public List<VendorBusinessHourDto> BusinessHours { get; set; }
}



public class CreateVendorDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [Phone]
    public string Phone { get; set; }
    
    public string PhoneNumber { get; set; } = string.Empty; // Alternative phone field
    
    [Url]
    public string Website { get; set; }
    
    public string WhatsApp { get; set; } = string.Empty;
    
    public string LogoUrl { get; set; } = string.Empty;
    
    public string CoverImageUrl { get; set; } = string.Empty;
    
    // Business registration details
    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string VatNumber { get; set; } = string.Empty;
    
    [Required]
    public int VendorTypeId { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    
    public double Rating { get; set; }
    
    public int ReviewCount { get; set; }
    
    // Flags for categorization
    public bool HasSpecialOffer { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPremium { get; set; } = false;
}

