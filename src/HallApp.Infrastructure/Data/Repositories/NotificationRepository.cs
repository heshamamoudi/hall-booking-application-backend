using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly DataContext _context;

        public NotificationRepository(DataContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public async Task<List<Notification>> GetByUserIdAsync(int appUserId)
        {
            return await _context.Notifications
                .Where(n => n.AppUserId == appUserId)
                .OrderByDescending(n => n.Created)
                .ToListAsync();
        }

        public async Task<Notification> GetByIdAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            return notification ?? throw new InvalidOperationException($"Notification with ID {notificationId} not found");
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

        public async Task MarkAllAsReadAsync(int appUserId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.AppUserId == appUserId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

        public async Task<int> GetUnreadCountAsync(int appUserId)
        {
            return await _context.Notifications
                .CountAsync(n => n.AppUserId == appUserId && !n.IsRead);
        }

        public async Task DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
            }
        }

        public async Task DeleteAllByUserAsync(int appUserId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.AppUserId == appUserId)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
        }
    }
}
