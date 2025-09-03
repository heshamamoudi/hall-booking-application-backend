using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(int appUserId, string title, string message, string type = "General")
        {
            try
            {
                _logger.LogInformation("Creating notification for AppUserId: {AppUserId}, Title: {Title}", appUserId, title);

                if (appUserId <= 0 || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Invalid notification data for User: {UserId}", appUserId);
                    return;
                }

                var notification = new Notification
                {
                    AppUserId = appUserId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Created = DateTime.UtcNow,
                    IsRead = false
                };

                await _unitOfWork.NotificationRepository.AddAsync(notification);
                await _unitOfWork.Complete();

                _logger.LogInformation("✅ Notification created successfully for User: {UserId}", appUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating notification for User: {UserId}", appUserId);
                // Don't rethrow - notification failures shouldn't break main workflow
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int appUserId)
        {
            try
            {
                return await _unitOfWork.NotificationRepository.GetByUserIdAsync(appUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for User: {UserId}", appUserId);
                return new List<Notification>();
            }
        }

        public async Task<Notification> GetNotificationByIdAsync(int notificationId)
        {
            try
            {
                var notification = await _unitOfWork.NotificationRepository.GetByIdAsync(notificationId);
                return notification ?? throw new InvalidOperationException($"Notification with ID {notificationId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification with ID: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                await _unitOfWork.NotificationRepository.MarkAsReadAsync(notificationId);
                await _unitOfWork.Complete();
                _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task MarkAllAsReadAsync(int appUserId)
        {
            try
            {
                await _unitOfWork.NotificationRepository.MarkAllAsReadAsync(appUserId);
                await _unitOfWork.Complete();
                _logger.LogInformation("All notifications marked as read for User: {UserId}", appUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for User: {UserId}", appUserId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(int appUserId)
        {
            try
            {
                return await _unitOfWork.NotificationRepository.GetUnreadCountAsync(appUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for User: {UserId}", appUserId);
                return 0;
            }
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            try
            {
                await _unitOfWork.NotificationRepository.DeleteAsync(notificationId);
                await _unitOfWork.Complete();
                _logger.LogInformation("Notification {NotificationId} deleted", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task DeleteAllUserNotificationsAsync(int appUserId)
        {
            try
            {
                await _unitOfWork.NotificationRepository.DeleteAllByUserAsync(appUserId);
                await _unitOfWork.Complete();
                _logger.LogInformation("All notifications deleted for User: {UserId}", appUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all notifications for User: {UserId}", appUserId);
                throw;
            }
        }

        public async Task SendBookingNotificationAsync(int appUserId, string bookingId, string status, string message)
        {
            var title = status.ToLower() switch
            {
                "approved" => "Booking Approved",
                "rejected" => "Booking Rejected",
                "cancelled" => "Booking Cancelled",
                "confirmed" => "Booking Confirmed",
                _ => "Booking Update"
            };

            await CreateNotificationAsync(appUserId, title, $"Booking #{bookingId}: {message}", "Booking");
        }

        public async Task SendSystemNotificationAsync(int appUserId, string title, string message)
        {
            await CreateNotificationAsync(appUserId, title, message, "System");
        }
    }
}
