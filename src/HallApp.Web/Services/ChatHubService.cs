using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HallApp.Web.Services;

/// <summary>
/// Implementation of IChatHubService for sending real-time chat messages
/// </summary>
public class ChatHubService : IChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatHubService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToConversationAsync(int conversationId, ChatMessage message)
    {
        var groupName = $"conversation_{conversationId}";

        // FIXED: Include SenderName by loading Sender navigation property
        var senderName = "System";
        if (message.Sender != null)
        {
            senderName = $"{message.Sender.FirstName} {message.Sender.LastName}";
        }

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = message.ConversationId,
                senderId = message.SenderId,
                senderName = senderName, // ADDED: Sender name for UI display
                senderType = message.SenderType,
                message = message.Message,
                messageType = message.MessageType,
                sentAt = message.SentAt,
                isRead = message.IsRead,
                isSystemMessage = message.IsSystemMessage,
                attachmentUrl = message.AttachmentUrl,
                attachmentName = message.AttachmentName
            });

        Console.WriteLine($"📨 SignalR: Message {message.Id} sent to conversation_{conversationId} (Sender: {senderName})");
    }

    public async Task SendTypingIndicatorAsync(int conversationId, string userName)
    {
        var groupName = $"conversation_{conversationId}";

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("UserTyping", new
            {
                ConversationId = conversationId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            });
    }

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
