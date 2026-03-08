namespace HallApp.Application.DTOs.Vendors;

public class VendorListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public VendorTypeDto? VendorType { get; set; }
    public VendorLocationDto? PrimaryLocation { get; set; }
}
