using System.ComponentModel.DataAnnotations;
using HallApp.Application.DTOs.Auth;

namespace HallApp.Application.DTOs.Vendors;

public class VendorManagerDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string CommercialRegistrationNumber { get; set; }
    public string VatNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<VendorListDto> Vendors { get; set; } = new();
    public UserDto AppUser { get; set; } = null!;
}

public class CreateVendorManagerDto
{
    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; }

    [StringLength(50)]
    public string VatNumber { get; set; }
}

public class UpdateVendorManagerDto
{
    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; }

    [StringLength(50)]
    public string VatNumber { get; set; }

    public List<int> VendorIds { get; set; } = new();
}

public class VendorManagerApprovalDto
{
    [Required]
    public bool IsApproved { get; set; }

    [StringLength(500)]
    public string RejectionReason { get; set; }
}
