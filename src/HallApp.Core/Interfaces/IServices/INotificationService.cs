using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface INotificationService
    {
        // Core notification operations
        Task CreateNotificationAsync(int appUserId, string title, string message, string type = "General");
        Task<List<Notification>> GetUserNotificationsAsync(int appUserId);
        Task<Notification> GetNotificationByIdAsync(int notificationId);
        
        // Read status management
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int appUserId);
        Task<int> GetUnreadCountAsync(int appUserId);
        
        // Deletion operations
        Task DeleteNotificationAsync(int notificationId);
        Task DeleteAllUserNotificationsAsync(int appUserId);
        
        // Specialized notification types
        Task SendBookingNotificationAsync(int appUserId, string bookingId, string status, string message);
        Task SendSystemNotificationAsync(int appUserId, string title, string message);
    }
}
