using HallApp.Core.Entities.ChatEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface IChatService
    {
        // Conversation Management
        Task<ChatConversation?> GetConversationByIdAsync(int id);
        Task<IEnumerable<ChatConversation>> GetAllConversationsAsync();
        Task<IEnumerable<ChatConversation>> GetCustomerConversationsAsync(int customerId);
        Task<IEnumerable<ChatConversation>> GetConversationsByCreatedByUserIdAsync(int userId);
        Task<IEnumerable<ChatConversation>> GetConversationsByConversationTypeAsync(string conversationType);
        Task<IEnumerable<ChatConversation>> GetAgentConversationsAsync(int agentId);
        Task<IEnumerable<ChatConversation>> GetUnassignedConversationsAsync();
        Task<ChatConversation> CreateConversationAsync(ChatConversation conversation);
        Task<ChatConversation> UpdateConversationAsync(ChatConversation conversation);
        Task<bool> DeleteConversationAsync(int id);

        // Conversation Actions
        Task<ChatConversation> AssignConversationAsync(int conversationId, int agentId);

        /// <summary>
        /// Close a conversation. Only admins/agents can close conversations.
        /// </summary>
        /// <param name="conversationId">The conversation ID to close</param>
        /// <param name="closedByUserId">The user ID who is closing the conversation</param>
        /// <param name="resolutionNotes">Optional resolution notes</param>
        /// <returns>The updated conversation</returns>
        Task<ChatConversation> CloseConversationAsync(int conversationId, int closedByUserId, string resolutionNotes);

        Task<ChatConversation> ReopenConversationAsync(int conversationId);
        Task<ChatConversation> TransferConversationAsync(int conversationId, int newAgentId);

        // Message Operations
        Task<ChatMessage> SendMessageAsync(int conversationId, int senderId, string message, string senderType);
        Task<IEnumerable<ChatMessage>> GetConversationMessagesAsync(int conversationId);
        Task<bool> MarkMessagesAsReadAsync(int conversationId, int userId);
        Task<bool> MarkMessagesAsUnreadAsync(int conversationId, int userId);
        Task<int> GetUnreadCountAsync(int conversationId, int userId);

        /// <summary>
        /// Check if a conversation is closed (no new messages allowed)
        /// </summary>
        Task<bool> IsConversationClosedAsync(int conversationId);

        // Authorization
        /// <summary>
        /// Check if a user has access to a specific conversation.
        /// Used by ChatHub for SignalR group authorization (CHAT-RISK-002).
        /// </summary>
        /// <param name="userId">The user ID requesting access</param>
        /// <param name="conversationId">The conversation ID to check</param>
        /// <param name="userRoles">The user's roles (e.g., "Admin", "HallManager", "VendorManager")</param>
        /// <returns>True if the user has access to the conversation</returns>
        Task<bool> CanAccessConversationAsync(int userId, int conversationId, IEnumerable<string> userRoles);

        #region Rating (Per-User Rating System)

        /// <summary>
        /// Legacy rating method - rates conversation with customer feedback (backward compatibility)
        /// </summary>
        Task<bool> RateConversationAsync(int conversationId, int rating, string feedback);

        /// <summary>
        /// Submit a rating for a conversation (per-user rating system).
        /// Only participants (non-admin) can rate. One rating per user per conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="userId">The user submitting the rating</param>
        /// <param name="rating">Rating 1-5</param>
        /// <param name="comment">Optional comment (sanitized for XSS)</param>
        /// <returns>The created rating</returns>
        Task<ChatRating> SubmitRatingAsync(int conversationId, int userId, int rating, string? comment);

        /// <summary>
        /// Get a user's rating for a conversation
        /// </summary>
        Task<ChatRating?> GetUserRatingAsync(int conversationId, int userId);

        /// <summary>
        /// Get all ratings for a conversation (admin view)
        /// </summary>
        Task<IEnumerable<ChatRating>> GetConversationRatingsAsync(int conversationId);

        /// <summary>
        /// Check if user can rate a conversation (must be participant, not admin, conversation must be closed)
        /// </summary>
        Task<bool> CanUserRateConversationAsync(int conversationId, int userId, IEnumerable<string> userRoles);

        #endregion

        #region Support Cases

        /// <summary>
        /// Create a support case which automatically creates a chat conversation
        /// </summary>
        Task<SupportCase> CreateSupportCaseAsync(SupportCase supportCase, string initialMessage);

        /// <summary>
        /// Get support case by ID
        /// </summary>
        Task<SupportCase?> GetSupportCaseByIdAsync(int caseId);

        /// <summary>
        /// Get support case by case number
        /// </summary>
        Task<SupportCase?> GetSupportCaseByCaseNumberAsync(string caseNumber);

        /// <summary>
        /// Get support cases for a user
        /// </summary>
        Task<IEnumerable<SupportCase>> GetUserSupportCasesAsync(int userId);

        /// <summary>
        /// Get all support cases (admin view)
        /// </summary>
        Task<IEnumerable<SupportCase>> GetAllSupportCasesAsync();

        /// <summary>
        /// Generate a unique case number (e.g., "CASE-2026-00001")
        /// </summary>
        Task<string> GenerateCaseNumberAsync();

        #endregion

        // Statistics
        Task<object> GetChatStatisticsAsync(DateTime? from = null, DateTime? to = null);
        Task<object> GetAgentPerformanceAsync(int agentId, DateTime? from = null, DateTime? to = null);
    }
}
