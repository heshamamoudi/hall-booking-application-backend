using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Champer.HallManager;

public class UserUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public DateTime DOB { get; set; } 
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public List<string> Roles { get; set; }
    
    [Required]
    public string Role { get; set; } = string.Empty;
    
    public bool Active { get; set; }
}
