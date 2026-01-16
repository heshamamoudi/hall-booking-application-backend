namespace HallApp.Application.DTOs.Halls.HallManager;

/// <summary>
/// DTO that combines AppUser and HallManager data for profile operations
/// Used for hall manager profile management, dashboard, and combined operations
/// </summary>
public class HallManagerProfileDto
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
    public DateTime? UserUpdated { get; set; }

    // From HallManager (business domain data)
    public int HallManagerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime HallManagerCreated { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Additional profile data
    public int TotalHalls { get; set; }
    public int ActiveHalls { get; set; }
    public int PendingApprovals { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalBookings { get; set; }
}
