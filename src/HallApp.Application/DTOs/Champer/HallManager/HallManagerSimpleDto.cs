namespace HallApp.Application.DTOs.Champer.HallManager;

/// <summary>
/// Simplified HallManager DTO without Halls to prevent circular references
/// </summary>
public class HallManagerSimpleDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string CompanyName { get; set; }
    public bool IsApproved { get; set; }
}
