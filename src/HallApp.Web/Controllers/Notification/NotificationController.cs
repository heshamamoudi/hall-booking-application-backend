using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Notifications;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace HallApp.Web.Controllers.Notification
{
    /// <summary>
    /// Notification management controller
    /// Handles user notifications, read status, and notification management
    /// </summary>
    [Authorize]
    [Route("api/notifications")]
    public class NotificationController : BaseApiController
    {
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public NotificationController(INotificationService notificationService, IMapper mapper)
        {
            _notificationService = notificationService;
            _mapper = mapper;
        }

        /// <summary>
        /// Create a new notification (Admin only)
        /// </summary>
        /// <param name="notificationDto">Notification data</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateNotification([FromBody] NotificationDto notificationDto)
        {
            try
            {
                if (notificationDto == null)
                {
                    return Error("Notification data is required", 400);
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error($"Invalid data: {errors}", 400);
                }

                await _notificationService.CreateNotificationAsync(
                    notificationDto.AppUserId, 
                    notificationDto.Title, 
                    notificationDto.Message, 
                    notificationDto.Type);
                    
                return Success("Notification created successfully");
            }
            catch (Exception ex)
            {
                return Error($"Failed to create notification: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get current user's notifications
        /// </summary>
        /// <returns>List of user's notifications</returns>
        [HttpGet("my-notifications")]
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationResponseDto>>>> GetMyNotifications()
        {
            try
            {
                var notifications = await _notificationService.GetUserNotificationsAsync(UserId);
                var notificationDtos = notifications.Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    AppUserId = n.AppUserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type ?? "General",
                    IsRead = n.IsRead,
                    CreatedAt = n.Created,
                    ReadAt = n.ReadAt
                });
                
                return Success(notificationDtos, "Notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationResponseDto>>($"Failed to retrieve notifications: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get notifications for a specific user (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's notifications</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationResponseDto>>>> GetUserNotifications(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return Error<IEnumerable<NotificationResponseDto>>("Invalid user ID", 400);
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                var notificationDtos = notifications.Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    AppUserId = n.AppUserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type ?? "General",
                    IsRead = n.IsRead,
                    CreatedAt = n.Created,
                    ReadAt = n.ReadAt
                });

                return Success(notificationDtos, $"Notifications for user {userId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationResponseDto>>($"Failed to retrieve user notifications: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>Success response</returns>
        [HttpPut("{notificationId:int}/read")]
        public async Task<ActionResult<ApiResponse>> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                if (notificationId <= 0)
                {
                    return Error("Invalid notification ID", 400);
                }

                // Get the notification to check if it belongs to the current user
                var notification = await _notificationService.GetNotificationByIdAsync(notificationId);

                // Check if user owns this notification (unless admin)
                if (!IsAdmin && notification.AppUserId != UserId)
                {
                    return Forbidden("You can only mark your own notifications as read");
                }

                await _notificationService.MarkAsReadAsync(notificationId);
                return Success("Notification marked as read");
            }
            catch (Exception ex)
            {
                return Error($"Failed to mark notification as read: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Mark all user's notifications as read
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllNotificationsAsRead()
        {
            try
            {
                await _notificationService.MarkAllAsReadAsync(UserId);
                return Success("All notifications marked as read");
            }
            catch (Exception ex)
            {
                return Error($"Failed to mark all notifications as read: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete a specific notification
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteNotification(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Error("Invalid notification ID", 400);
                }

                // Get the notification to check permissions
                var notification = await _notificationService.GetNotificationByIdAsync(id);

                // Check if user owns this notification (unless admin)
                if (!IsAdmin && notification.AppUserId != UserId)
                {
                    return Error("You can only delete your own notifications", 403);
                }

                // Delete the notification
                await _notificationService.DeleteNotificationAsync(id);

                return Success($"Notification with ID {id} has been deleted");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete notification: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete all notifications for current user
        /// </summary>
        /// <returns>Success response</returns>
        [HttpDelete("my-notifications")]
        public async Task<ActionResult<ApiResponse>> DeleteMyNotifications()
        {
            try
            {
                await _notificationService.DeleteAllUserNotificationsAsync(UserId);
                return Success("All your notifications have been deleted");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete notifications: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Delete all notifications for a specific user (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Updated notifications list</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteUserNotifications(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return Error("Invalid user ID", 400);
                }

                // Delete all notifications for the user
                await _notificationService.DeleteAllUserNotificationsAsync(userId);

                return Success($"All notifications for user with ID {userId} have been deleted");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete user notifications: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get unread notifications count for current user
        /// </summary>
        /// <returns>Unread notifications count</returns>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadNotificationsCount()
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync(UserId);
                return Success(count, "Unread notifications count retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<int>($"Failed to retrieve unread notifications count: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get notification statistics (Admin only)
        /// </summary>
        /// <returns>Notification statistics</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetNotificationStatistics()
        {
            try
            {
                var notifications = await _notificationService.GetUserNotificationsAsync(1); // Admin overview
                var unreadCount = await _notificationService.GetUnreadCountAsync(1);
                object stats = new
                {
                    TotalNotifications = notifications.Count,
                    UnreadCount = unreadCount,
                    SystemNotifications = notifications.Count(n => n.Title.Contains("System")),
                    BookingNotifications = notifications.Count(n => n.Title.Contains("Booking")),
                    ReviewNotifications = notifications.Count(n => n.Title.Contains("Review"))
                };
                return Success(stats, "Notification statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve notification statistics: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Send a test notification to current user
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("test")]
        public async Task<ActionResult<ApiResponse>> SendTestNotification()
        {
            try
            {
                var notificationTypes = new[] { "Booking", "Payment", "Review", "Hall", "System" };
                var randomType = notificationTypes[new Random().Next(notificationTypes.Length)];
                
                var title = $"Test {randomType} Notification";
                var message = $"This is a test {randomType.ToLower()} notification sent at {DateTime.Now:yyyy-MM-dd HH:mm:ss}. Testing real-time updates via SignalR!";
                
                await _notificationService.CreateNotificationAsync(
                    UserId,
                    title,
                    message,
                    randomType);
                
                return Success($"Test notification sent successfully! Type: {randomType}");
            }
            catch (Exception ex)
            {
                return Error($"Failed to send test notification: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Send a custom test notification to current user
        /// </summary>
        /// <param name="request">Test notification request</param>
        /// <returns>Success response</returns>
        [HttpPost("test/custom")]
        public async Task<ActionResult<ApiResponse>> SendCustomTestNotification([FromBody] TestNotificationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Error("Request data is required", 400);
                }

                var title = string.IsNullOrEmpty(request.Title) ? "Test Notification" : request.Title;
                var message = string.IsNullOrEmpty(request.Message) ? "This is a test notification" : request.Message;
                var type = string.IsNullOrEmpty(request.Type) ? "Info" : request.Type;
                
                await _notificationService.CreateNotificationAsync(
                    UserId,
                    title,
                    message,
                    type);
                
                return Success($"Custom test notification sent successfully!");
            }
            catch (Exception ex)
            {
                return Error($"Failed to send custom test notification: {ex.Message}", 500);
            }
        }
    }

    /// <summary>
    /// Test notification request model
    /// </summary>
    public class TestNotificationRequest
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
    }
}
