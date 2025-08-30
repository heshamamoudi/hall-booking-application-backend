#nullable enable
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

        public Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int appUserId)
        {
            return await _context.Notifications
                .Where(n => n.AppUserId == appUserId)
                .OrderByDescending(n => n.Created)
                .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(int notificationId)
        {
            return await _context.Notifications.FindAsync(notificationId);
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

        public Task DeleteNotificationAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            return Task.CompletedTask;
        }

        public async Task DeleteNotificationsByUserAsync(int appUserId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.AppUserId == appUserId)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
        }
    }
}
