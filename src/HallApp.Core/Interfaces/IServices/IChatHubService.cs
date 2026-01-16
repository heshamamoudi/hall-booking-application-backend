using HallApp.Core.Entities.ChatEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// Service for sending real-time chat messages via SignalR
/// </summary>
public interface IChatHubService
{
    /// <summary>
    /// Send real-time message to conversation participants via SignalR
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="message">The message to send</param>
    Task SendMessageToConversationAsync(int conversationId, ChatMessage message);

    /// <summary>
    /// Send typing indicator to conversation participants
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="userName">The name of the user typing</param>
    Task SendTypingIndicatorAsync(int conversationId, string userName);

    /// <summary>
    /// Send message read notification to conversation participants
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="userId">The ID of the user who read the messages</param>
    Task SendMessageReadNotificationAsync(int conversationId, int userId);
}
