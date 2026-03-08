using HallApp.Core.Entities.NotificationEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications.
/// Handles notification delivery to connected users.
/// </summary>
/// <remarks>
/// SignalR Events Emitted:
/// - ReceiveNotification: { id, title, message, type, createdAt, isRead }
/// - NotificationRead: { notificationId, timestamp }
/// - AllNotificationsRead: { timestamp }
/// - UnreadCountUpdated: { count }
///
/// Client Methods Expected:
/// - JoinUserGroup(): Explicitly join user's notification group (called after reconnection)
/// - MarkNotificationRead(notificationId: int): Notify server of read action (for sync across tabs)
/// </remarks>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user ID from claims with multiple fallbacks
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? Context.User?.FindFirst("nameid")
                         ?? Context.User?.FindFirst("sub")
                         ?? Context.User?.FindFirst("id");

        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Called when a client connects. Adds them to their personal notification group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            _logger.LogWarning("NotificationHub: Connection attempted without valid user ID. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort(); // Terminate connection for unauthenticated users
            return;
        }

        // Add user to their personal notification group (group name = userId as string)
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());

        _logger.LogInformation("NotificationHub: User {UserId} connected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (exception != null)
        {
            _logger.LogWarning(exception, "NotificationHub: User {UserId} disconnected with error. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("NotificationHub: User {UserId} disconnected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client-callable method for explicitly joining user group (useful after reconnection)
    /// </summary>
    public async Task JoinUserGroup()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            _logger.LogWarning("NotificationHub: JoinUserGroup called without valid user. ConnectionId: {ConnectionId}", Context.ConnectionId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());
        _logger.LogDebug("NotificationHub: User {UserId} explicitly joined notification group", userId);
    }

    /// <summary>
    /// Client-callable method to sync notification read status across devices/tabs
    /// </summary>
    public async Task MarkNotificationRead(int notificationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        // Broadcast to all user's connections that this notification was read
        await Clients.OthersInGroup(userId.Value.ToString()).SendAsync("NotificationRead", new
        {
            NotificationId = notificationId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Client-callable method to sync "mark all as read" across devices/tabs
    /// </summary>
    public async Task MarkAllNotificationsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        // Broadcast to all user's connections
        await Clients.OthersInGroup(userId.Value.ToString()).SendAsync("AllNotificationsRead", new
        {
            Timestamp = DateTime.UtcNow
        });
    }
}
