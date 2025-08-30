using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface INotificationService
    {
        // Method to create and send a notification to an AppUser
        Task CreateAndSendNotificationAsync(int appUserId, string title, string message);
        Task CreateAndSendNotification(object notificationDto);

        // Method to retrieve notifications for an AppUser
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int appUserId);
        Task<IEnumerable<NotificationResponseDto>> GetUserNotifications(int userId);
        Task<IEnumerable<NotificationResponseDto>> GetUserNotifications(string userId);

        // Method to mark a notification as read
        Task MarkAsReadAsync(int notificationId);
        Task MarkAsRead(int notificationId);
        
        // Method to mark all notifications as read for a user
        Task MarkAllAsReadAsync(int appUserId);

        Task DeleteNotificationsByUserAsync(int appUserId);
        Task DeleteNotificationsByUser(int userId);

        Task DeleteNotificationByIdAsync(int notificationId);
        Task DeleteNotificationById(int notificationId);
        
        Task<Notification> GetNotificationByIdAsync(int notificationId);
        Task<NotificationResponseDto?> GetNotificationById(int notificationId);
        
        Task<int> GetUnreadNotificationsCount(int userId);
        Task<int> GetUnreadNotificationsCount(string userId);
        Task SendBookingCancellationNotificationAsync(string bookingId, string customerId);
        Task SendBookingStatusUpdateNotificationAsync(object bookingDto);
    }

    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
