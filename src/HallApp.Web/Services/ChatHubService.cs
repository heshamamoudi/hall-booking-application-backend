using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HallApp.Web.Services;

/// <summary>
/// Implementation of IChatHubService for sending real-time chat messages via SignalR.
/// This service is used by ChatService to broadcast messages to connected clients.
/// </summary>
public class ChatHubService : IChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatHubService> _logger;

    public ChatHubService(IHubContext<ChatHub> hubContext, ILogger<ChatHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Sends a new message to all participants in a conversation.
    ///
    /// FIX CHAT-BUG-001: The isRead field is now set to false for all recipients.
    /// The frontend should determine if the message is "read" by the current user based on:
    /// 1. If senderId matches the current user's ID, it's their own message (always considered "read")
    /// 2. Otherwise, it's unread until they explicitly mark it as read
    ///
    /// This is the correct behavior for per-user read tracking.
    /// </summary>
    public async Task SendMessageToConversationAsync(int conversationId, ChatMessage message)
    {
        var groupName = $"conversation_{conversationId}";

        // Build sender name from navigation property
        var senderName = "System";
        if (message.Sender != null)
        {
            var firstName = message.Sender.FirstName?.Trim() ?? "";
            var lastName = message.Sender.LastName?.Trim() ?? "";
            senderName = !string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName)
                ? $"{firstName} {lastName}".Trim()
                : message.Sender.UserName ?? message.Sender.Email ?? "User";
        }

        // Sanitize message content to prevent XSS
        var sanitizedMessage = System.Net.WebUtility.HtmlEncode(message.Message ?? "");

        // FIX CHAT-BUG-001: Always broadcast isRead=false for new messages
        // The frontend determines read status based on whether the current user is the sender
        // Sender's own messages are always "read" for them, but unread for recipients
        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderId = message.SenderId,
                senderName = senderName,
                senderType = message.SenderType,
                message = sanitizedMessage,
                messageType = message.MessageType,
                sentAt = message.SentAt,
                isRead = false, // FIX: Always false for broadcast - frontend checks if senderId == currentUserId
                readAt = (DateTime?)null, // No read timestamp for new messages
                isSystemMessage = message.IsSystemMessage,
                attachmentUrl = message.AttachmentUrl,
                attachmentName = message.AttachmentName,
                attachmentSize = message.AttachmentSize
            });

        _logger.LogDebug("SignalR: Message {MessageId} sent to conversation {ConversationId}", message.Id, conversationId);
    }

    /// <summary>
    /// Sends a typing indicator to other participants in a conversation
    /// </summary>
    public async Task SendTypingIndicatorAsync(int conversationId, string userName)
    {
        var groupName = $"conversation_{conversationId}";

        // Sanitize userName
        var sanitizedUserName = System.Net.WebUtility.HtmlEncode(userName ?? "User");

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("UserTyping", new
            {
                ConversationId = conversationId,
                UserName = sanitizedUserName,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies participants that messages have been read
    /// </summary>
    public async Task SendMessageReadNotificationAsync(int conversationId, int userId)
    {
        var groupName = $"conversation_{conversationId}";

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("MessagesRead", new
            {
                ConversationId = conversationId,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
    }
}
