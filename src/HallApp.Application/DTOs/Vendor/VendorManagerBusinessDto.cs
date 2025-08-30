namespace HallApp.Application.DTOs.Vendor;

/// <summary>
/// DTO for VendorManager business domain operations only
/// Contains only business-related vendor manager data
/// </summary>
public class VendorManagerBusinessDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string? CommercialRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
