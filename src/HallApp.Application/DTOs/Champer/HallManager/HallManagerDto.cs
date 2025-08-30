using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.HallManager;

public class HallManagerDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Password { get; set; }
    public bool Active { get; set; } 
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<HallDto> Halls { get; set; }
    public int AppUserID { get; set; }
}
