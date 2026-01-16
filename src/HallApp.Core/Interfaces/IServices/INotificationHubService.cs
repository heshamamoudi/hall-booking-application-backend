using HallApp.Core.Entities.NotificationEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationHubService
{
    /// <summary>
    /// Send real-time notification to a specific user via SignalR
    /// </summary>
    /// <param name="userId">The user ID to send notification to</param>
    /// <param name="notification">The notification to send</param>
    Task SendNotificationToUserAsync(int userId, Notification notification);
}
