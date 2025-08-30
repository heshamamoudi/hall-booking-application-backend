namespace HallApp.Application.DTOs.HallManager;

/// <summary>
/// DTO for HallManager business domain operations only
/// Contains only business-related hall manager data
/// </summary>
public class HallManagerBusinessDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
