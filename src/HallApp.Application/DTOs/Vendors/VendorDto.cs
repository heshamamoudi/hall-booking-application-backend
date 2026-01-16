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
