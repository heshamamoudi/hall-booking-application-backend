namespace HallApp.Application.DTOs.Halls.HallManager;

public class UserCreateDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Password { get; set; }
    public bool Active { get; set; }
    public DateTime DOB { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<string> Roles { get; set; } 
}
