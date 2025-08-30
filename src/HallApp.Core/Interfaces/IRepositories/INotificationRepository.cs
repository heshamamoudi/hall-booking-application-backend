using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Interfaces.IRepositories
{
    public interface INotificationRepository
    {
        Task CreateNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsByUserIdAsync(int appUserId);
        Task<Notification?> GetNotificationByIdAsync(int notificationId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int appUserId);
        Task DeleteNotificationAsync(Notification notification);
        Task DeleteNotificationsByUserAsync(int appUserId);
    }
}
