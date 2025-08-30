namespace HallApp.Application.DTOs.Champer.HallManager;

public class UserUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public DateTime DOB { get; set; } 
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<string> Roles { get; set; }
    public bool Active { get; set; }
}
