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

    /// <summary>
    /// Send chat closed notification to all participants.
    /// After this event, participants should disable message input and show closed status.
    /// </summary>
    /// <param name="conversationId">The conversation ID that was closed</param>
    /// <param name="closedByUserId">The user ID who closed the conversation</param>
    /// <param name="closedByName">The name of the user who closed the conversation</param>
    /// <param name="closedAt">The timestamp when the conversation was closed</param>
    /// <param name="resolutionNotes">Optional resolution notes</param>
    Task SendChatClosedNotificationAsync(int conversationId, int closedByUserId, string closedByName, DateTime closedAt, string? resolutionNotes);

    /// <summary>
    /// Send rating submitted notification to conversation participants.
    /// Shows other participants that someone rated the conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="userId">The user who submitted the rating</param>
    /// <param name="userName">The name of the user who submitted the rating</param>
    /// <param name="rating">The rating value (1-5)</param>
    Task SendRatingSubmittedNotificationAsync(int conversationId, int userId, string userName, int rating);
}
