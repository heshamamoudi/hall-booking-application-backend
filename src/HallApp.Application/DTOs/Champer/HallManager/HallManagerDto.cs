using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.HallManager;

public class HallManagerDto
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string CompanyName { get; set; }
    public string CommercialRegistrationNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int AppUserId { get; set; }
    public List<HallSimpleDto> Halls { get; set; } = new List<HallSimpleDto>();
}
