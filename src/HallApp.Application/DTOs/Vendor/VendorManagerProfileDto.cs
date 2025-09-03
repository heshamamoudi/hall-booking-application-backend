namespace HallApp.Application.DTOs.Vendor;

/// <summary>
/// DTO that combines AppUser and VendorManager data for profile operations
/// Used for vendor manager profile management, dashboard, and combined operations
/// </summary>
public class VendorManagerProfileDto
{
    // From AppUser (identity/auth data)
    public int AppUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool Active { get; set; }
    public DateTime UserCreated { get; set; }
    public DateTime UserUpdated { get; set; }

    // From VendorManager (business domain data)
    public int VendorManagerId { get; set; }
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime VendorManagerCreated { get; set; }
    public DateTime ApprovedAt { get; set; }

    // Additional profile data
    public int TotalVendors { get; set; }
    public int ActiveVendors { get; set; }
    public int PendingApprovals { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
