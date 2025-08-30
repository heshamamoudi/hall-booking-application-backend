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
    [Route("api/v1/notifications")]
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

                await _notificationService.CreateAndSendNotification(notificationDto);
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
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetMyNotifications()
        {
            try
            {
                var notificationResponses = await _notificationService.GetUserNotifications(UserId);
                var notifications = _mapper.Map<IEnumerable<NotificationDto>>(notificationResponses) ?? new List<NotificationDto>();              
                if (notifications == null || !notifications.Any())
                {
                    return Success<IEnumerable<NotificationDto>>(new List<NotificationDto>(), "No notifications found");
                }

                return Success(notifications, "Notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationDto>>($"Failed to retrieve notifications: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get notifications for a specific user (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's notifications</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetUserNotifications(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return Error<IEnumerable<NotificationDto>>("Invalid user ID", 400);
                }

                var notificationResponses = await _notificationService.GetUserNotifications(userId);
                var notifications = _mapper.Map<IEnumerable<NotificationDto>>(notificationResponses) ?? new List<NotificationDto>();
                
                if (!notifications.Any())
                {
                    return Success<IEnumerable<NotificationDto>>(new List<NotificationDto>(), $"No notifications found for user {userId}");
                }

                return Success(notifications, $"Notifications for user {userId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationDto>>($"Failed to retrieve user notifications: {ex.Message}", 500);
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
                var notification = await _notificationService.GetNotificationById(notificationId);
                if (notification == null)
                {
                    return NotFound($"Notification with ID {notificationId} not found");
                }

                // Check if user owns this notification (unless admin)
                if (!IsAdmin && notification.AppUserId != UserId)
                {
                    return Forbidden("You can only mark your own notifications as read");
                }

                await _notificationService.MarkAsRead(notificationId);
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
        /// <returns>Updated notifications list</returns>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> DeleteNotification(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Error<IEnumerable<NotificationDto>>("Invalid notification ID", 400);
                }

                // Get the notification to check permissions and user ID
                var notification = await _notificationService.GetNotificationById(id);
                if (notification == null)
                {
                    return Error<IEnumerable<NotificationDto>>($"Notification with ID {id} not found", 404);
                }

                // Check if user owns this notification (unless admin)
                if (!IsAdmin && notification.AppUserId != UserId)
                {
                    return Error<IEnumerable<NotificationDto>>("You can only access your own notifications", 403);
                }

                var appUserId = notification.AppUserId;

                // Delete the notification
                await _notificationService.DeleteNotificationById(id);

                // Fetch updated notifications
                var updatedNotifications = await _notificationService.GetUserNotifications(appUserId);

                // Convert to DTOs
                var notificationDtos = _mapper.Map<IEnumerable<NotificationDto>>(updatedNotifications);

                return Success(notificationDtos ?? new List<NotificationDto>(), 
                    $"Notification with ID {id} has been deleted");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationDto>>($"Failed to delete notification: {ex.Message}", 500);
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
                await _notificationService.DeleteNotificationsByUser(UserId);
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
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> DeleteUserNotifications(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return Error<IEnumerable<NotificationDto>>("Invalid user ID", 400);
                }

                // Delete all notifications for the user
                await _notificationService.DeleteNotificationsByUser(userId);

                // Fetch updated notifications (should be empty)
                var updatedNotifications = await _notificationService.GetUserNotifications(userId);
                var notificationDtos = _mapper.Map<IEnumerable<NotificationDto>>(updatedNotifications);

                return Success(notificationDtos ?? new List<NotificationDto>(), 
                    $"All notifications for user with ID {userId} have been deleted");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<NotificationDto>>($"Failed to delete user notifications: {ex.Message}", 500);
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
                var count = await _notificationService.GetUnreadNotificationsCount(UserId);
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
                var unreadCount = await _notificationService.GetUnreadNotificationsCount(1);
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
    }
}
