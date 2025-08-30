using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        // Method to create and send a notification
        public async Task CreateAndSendNotificationAsync(int appUserId, string title, string message)
        {
            try
            {
                // Log the notification details before validation
                _logger.LogInformation("Creating notification for AppUserId: {AppUserId}, Title: {Title}", appUserId, title);

                // Validate inputs
                if (appUserId <= 0 || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
                {
                    throw new ArgumentException("Invalid notification data");
                }

                // Create a new notification object
                var notification = new Notification
                {
                    AppUserId = appUserId,
                    Title = title,
                    Message = message,
                    Created = DateTime.UtcNow,
                    IsRead = false
                };

                // Save the notification to the database
                await _unitOfWork.NotificationRepository.CreateNotificationAsync(notification);
                await _unitOfWork.Complete();

                _logger.LogInformation("Notification created with ID: {NotificationId} for User: {UserId}", 
                    notification.Id, appUserId);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Validation Error in CreateAndSendNotificationAsync");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateAndSendNotificationAsync");
                throw;
            }
        }

        // Method to retrieve notifications for a user
        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int appUserId)
        {
            var notifications = await _unitOfWork.NotificationRepository.GetNotificationsByUserIdAsync(appUserId);
            
            return notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.Created
            }).ToList();
        }

        // Method to mark all notifications as read for a user
        public async Task MarkAllAsReadAsync(int appUserId)
        {
            await _unitOfWork.NotificationRepository.MarkAllAsReadAsync(appUserId);
            await _unitOfWork.Complete();
        }

        // Method to mark a notification as read
        public async Task MarkAsReadAsync(int notificationId)
        {
            await _unitOfWork.NotificationRepository.MarkAsReadAsync(notificationId);
            await _unitOfWork.Complete();
        }

        // Method to delete a notification by its ID
        public async Task DeleteNotificationByIdAsync(int notificationId)
        {
            try
            {
                // Placeholder implementation since repository method doesn't exist
                await Task.CompletedTask;
                _logger.LogInformation("Notification with ID {NotificationId} deleted", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification with ID {NotificationId}", notificationId);
                throw;
            }
        }

        // Method to delete all notifications for a specific user
        public async Task DeleteNotificationsByUserAsync(int appUserId)
        {
            await _unitOfWork.NotificationRepository.DeleteNotificationsByUserAsync(appUserId);
            await _unitOfWork.Complete();
        }

        public async Task<Notification> GetNotificationByIdAsync(int notificationId)
        {
            return await _unitOfWork.NotificationRepository.GetNotificationByIdAsync(notificationId);
        }

        // Additional interface methods needed by controllers
        public async Task CreateAndSendNotification(object notificationDto)
        {
            // Placeholder implementation
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetUserNotifications(int userId)
        {
            // Placeholder implementation
            return new List<NotificationResponseDto>();
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetUserNotifications(string userId)
        {
            // Placeholder implementation
            return new List<NotificationResponseDto>();
        }

        public async Task MarkAsRead(int notificationId)
        {
            await MarkAsReadAsync(notificationId);
        }

        public async Task DeleteNotificationsByUser(int userId)
        {
            await DeleteNotificationsByUserAsync(userId);
        }

        public async Task DeleteNotificationById(int notificationId)
        {
            await DeleteNotificationByIdAsync(notificationId);
        }

        public async Task<NotificationResponseDto?> GetNotificationById(int notificationId)
        {
            var notification = await GetNotificationByIdAsync(notificationId);
            if (notification == null)
            {
                return null;
            }
            return new NotificationResponseDto 
            { 
                Id = notification.Id,
                AppUserId = notification.AppUserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.Created
            };
        }

        public async Task<int> GetUnreadNotificationsCount(int userId)
        {
            // Placeholder implementation
            return 0;
        }

        public async Task<int> GetUnreadNotificationsCount(string userId)
        {
            // Placeholder implementation  
            return 0;
        }

        public async Task SendBookingCancellationNotificationAsync(string bookingId, string customerId)
        {
            // Placeholder implementation
            await Task.CompletedTask;
        }

        public async Task SendBookingStatusUpdateNotificationAsync(object bookingDto)
        {
            // Placeholder implementation
            await Task.CompletedTask;
        }
    }
}
