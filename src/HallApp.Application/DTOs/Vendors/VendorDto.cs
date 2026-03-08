using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Vendors;

public class VendorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    
    // Business registration details
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int VendorManagerId { get; set; }
    public int VendorTypeId { get; set; }
    public VendorTypeDto? VendorType { get; set; }
    public List<ServiceItemDto> ServiceItems { get; set; } = [];
    public VendorLocationDto? Location { get; set; }
    public List<VendorLocationDto> Locations { get; set; } = [];
    public List<VendorBusinessHourDto> BusinessHours { get; set; } = [];
}
