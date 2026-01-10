namespace HallApp.Application.DTOs.Vendor;

/// <summary>
/// DTO for VendorManager - simplified link entity
/// VendorManager just links AppUser to managed Vendors
/// Business properties (approval, registration, VAT) are on Vendor entity
/// </summary>
public class VendorManagerBusinessDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
