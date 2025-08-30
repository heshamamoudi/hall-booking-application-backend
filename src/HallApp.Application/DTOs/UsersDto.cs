namespace HallApp.Application.DTOs;

public class UsersDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DOB { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool Active { get; set; }
}
