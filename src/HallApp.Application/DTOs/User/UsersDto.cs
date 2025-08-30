namespace HallApp.Application.DTOs.User
{
    public class UsersDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DOB { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool Active { get; set; }
    }
}
