using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

/// <summary>
/// SignalR Hub for real-time chat messaging.
/// Handles connection management, group membership, typing indicators, and message delivery.
/// </summary>
/// <remarks>
/// SignalR Events Emitted:
/// - ReceiveMessage: { id, conversationId, senderId, senderName, senderType, message, messageType, sentAt, isRead, isSystemMessage, attachmentUrl?, attachmentName? }
/// - UserTyping: { conversationId, userName, timestamp }
/// - MessagesRead: { conversationId, userId, timestamp }
/// - UserStoppedTyping: { conversationId, userName, timestamp }
///
/// Client Methods Expected:
/// - JoinConversation(conversationId: int): Join a conversation group to receive messages
/// - LeaveConversation(conversationId: int): Leave a conversation group
/// - SendTypingIndicator(conversationId: int, userName: string): Notify others you're typing
/// - StopTypingIndicator(conversationId: int, userName: string): Notify others you stopped typing
/// </remarks>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatService _chatService;

    public ChatHub(ILogger<ChatHub> logger, IChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
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

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            _logger.LogWarning("ChatHub: Connection attempted without valid user ID. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort(); // Terminate connection for unauthenticated users
            return;
        }

        // Add user to their personal group for direct messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        _logger.LogInformation("ChatHub: User {UserId} connected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (exception != null)
        {
            _logger.LogWarning(exception, "ChatHub: User {UserId} disconnected with error. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("ChatHub: User {UserId} disconnected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific conversation group to receive real-time messages
    /// </summary>
    /// <remarks>
    /// SECURITY (CHAT-RISK-002): Server-side authorization check added to prevent conversation hijacking.
    /// Validates that the user is a participant in the conversation before allowing group membership.
    /// </remarks>
    public async Task JoinConversation(int conversationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("ChatHub: JoinConversation called without valid user. ConnectionId: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("JoinConversationError", new { Error = "Unauthorized", Message = "User not authenticated" });
            return;
        }

        // SECURITY (CHAT-RISK-002): Verify user has access to this conversation before adding to group
        var userRoles = GetCurrentUserRoles();
        var canAccess = await _chatService.CanAccessConversationAsync(userId.Value, conversationId, userRoles);

        if (!canAccess)
        {
            _logger.LogWarning("ChatHub: User {UserId} denied access to conversation {ConversationId}. Possible hijacking attempt.", userId, conversationId);
            await Clients.Caller.SendAsync("JoinConversationError", new { Error = "Forbidden", Message = "Access denied to this conversation" });
            return;
        }

        var groupName = $"conversation_{conversationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("ChatHub: User {UserId} joined conversation {ConversationId}", userId, conversationId);
        await Clients.Caller.SendAsync("JoinedConversation", new { ConversationId = conversationId, Success = true });
    }

    /// <summary>
    /// Gets the current user's roles from claims
    /// </summary>
    private IEnumerable<string> GetCurrentUserRoles()
    {
        return Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Leave a specific conversation group
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        var userId = GetCurrentUserId();
        var groupName = $"conversation_{conversationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("ChatHub: User {UserId} left conversation {ConversationId}", userId, conversationId);
    }

    /// <summary>
    /// Notify other participants that the user is typing
    /// </summary>
    public async Task SendTypingIndicator(int conversationId, string userName)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        // Sanitize userName to prevent XSS
        var sanitizedUserName = System.Net.WebUtility.HtmlEncode(userName ?? "User");

        var groupName = $"conversation_{conversationId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            ConversationId = conversationId,
            UserName = sanitizedUserName,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify other participants that the user stopped typing
    /// </summary>
    public async Task StopTypingIndicator(int conversationId, string userName)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        var sanitizedUserName = System.Net.WebUtility.HtmlEncode(userName ?? "User");

        var groupName = $"conversation_{conversationId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
        {
            ConversationId = conversationId,
            UserName = sanitizedUserName,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send message read notification to other participants
    /// </summary>
    /// <remarks>
    /// Called when user marks messages as read in a conversation.
    /// Other participants receive this to update their UI (e.g., show read receipts).
    /// </remarks>
    public async Task SendMessageReadNotification(int conversationId, int userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return;

        var groupName = $"conversation_{conversationId}";
        await Clients.OthersInGroup(groupName).SendAsync("MessagesRead", new
        {
            ConversationId = conversationId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }
}
