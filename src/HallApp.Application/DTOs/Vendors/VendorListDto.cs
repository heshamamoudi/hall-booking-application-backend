namespace HallApp.Application.DTOs.Vendors;

public class VendorListDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogoUrl { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public VendorTypeDto VendorType { get; set; }
    public VendorLocationDto PrimaryLocation { get; set; }
}
