using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Interfaces.IRepositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(int appUserId);
        Task<Notification> GetByIdAsync(int notificationId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int appUserId);
        Task<int> GetUnreadCountAsync(int appUserId);
        Task DeleteAsync(int notificationId);
        Task DeleteAllByUserAsync(int appUserId);
    }
}
