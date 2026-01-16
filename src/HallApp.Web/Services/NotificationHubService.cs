using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HallApp.Web.Services;

/// <summary>
/// Implementation of INotificationHubService for sending real-time notifications
/// </summary>
public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationToUserAsync(int userId, Notification notification)
    {
        await _hubContext.Clients
            .Group(userId.ToString())
            .SendAsync("ReceiveNotification", new
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.Created,
                IsRead = notification.IsRead
            });
    }
}
