using HallApp.Application.DTOs.Halls.Hall;

namespace HallApp.Application.DTOs.Halls.HallManager;

public class HallManagerDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int AppUserId { get; set; }
    public List<HallSimpleDto> Halls { get; set; } = new List<HallSimpleDto>();
}
