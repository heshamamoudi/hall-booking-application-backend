namespace HallApp.Application.DTOs.Notifications;

public class NotificationDto
{
    public int AppUserId { get; set; }  // Changed from CustomerId to AppUserId
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
}
