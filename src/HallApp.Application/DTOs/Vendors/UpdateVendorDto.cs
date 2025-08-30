namespace HallApp.Application.DTOs.Vendors;

public class UpdateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int VendorTypeId { get; set; }
    public bool IsActive { get; set; } = true;
}
