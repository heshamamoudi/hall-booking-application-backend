using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class CreateVendorDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Url]
    public string Website { get; set; } = string.Empty;
    
    public string WhatsApp { get; set; } = string.Empty;
    
    public string LogoUrl { get; set; } = string.Empty;
    
    public string CoverImageUrl { get; set; } = string.Empty;
    
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
    
    public bool HasSpecialOffer { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPremium { get; set; } = false;
}
