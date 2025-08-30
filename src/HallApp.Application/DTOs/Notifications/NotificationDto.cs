namespace HallApp.Application.DTOs.Notifications;

public class NotificationDto
{
    public int AppUserId { get; set; }  // Changed from CustomerId to AppUserId
    public string Title { get; set; }
    public string Message { get; set; }
}
