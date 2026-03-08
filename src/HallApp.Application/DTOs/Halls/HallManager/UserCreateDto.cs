namespace HallApp.Application.DTOs.Halls.HallManager;

public class UserCreateDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime DOB { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<string> Roles { get; set; } = [];
}
