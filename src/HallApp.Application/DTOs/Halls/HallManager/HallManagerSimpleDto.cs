namespace HallApp.Application.DTOs.Halls.HallManager;

/// <summary>
/// Simplified HallManager DTO without Halls to prevent circular references
/// </summary>
public class HallManagerSimpleDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
