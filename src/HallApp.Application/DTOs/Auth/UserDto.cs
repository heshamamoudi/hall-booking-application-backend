namespace HallApp.Application.DTOs.Auth
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
