namespace HallApp.Application.DTOs.Notifications;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
