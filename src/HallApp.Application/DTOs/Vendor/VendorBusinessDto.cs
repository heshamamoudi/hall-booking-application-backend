namespace HallApp.Application.DTOs.Vendor;

/// <summary>
/// DTO for Vendor business domain operations only
/// Contains only business-related vendor data
/// </summary>
public class VendorBusinessDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int VendorTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
