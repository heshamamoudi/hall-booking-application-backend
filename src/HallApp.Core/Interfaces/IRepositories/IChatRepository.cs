using HallApp.Core.Entities.ChatEntities;

namespace HallApp.Core.Interfaces.IRepositories
{
    /// <summary>
    /// Repository interface for Chat Conversations
    /// </summary>
    public interface IChatRepository
    {
        // Conversation CRUD
        Task<ChatConversation> GetConversationByIdAsync(int id);
        Task<IEnumerable<ChatConversation>> GetAllConversationsAsync();
        Task<IEnumerable<ChatConversation>> GetConversationsByCustomerIdAsync(int customerId);
        Task<IEnumerable<ChatConversation>> GetConversationsByCreatedByUserIdAsync(int userId);
        Task<IEnumerable<ChatConversation>> GetConversationsByConversationTypeAsync(string conversationType);
        Task<IEnumerable<ChatConversation>> GetConversationsByAgentIdAsync(int agentId);
        Task<IEnumerable<ChatConversation>> GetConversationsByStatusAsync(string status);
        Task<IEnumerable<ChatConversation>> GetUnassignedConversationsAsync();
        Task<IEnumerable<ChatConversation>> GetConversationsByPriorityAsync(string priority);
        Task<ChatConversation> CreateConversationAsync(ChatConversation conversation);
        Task<ChatConversation> UpdateConversationAsync(ChatConversation conversation);
        Task<bool> DeleteConversationAsync(int id);

        // Message Operations
        Task<ChatMessage> GetMessageByIdAsync(int id);
        Task<IEnumerable<ChatMessage>> GetMessagesByConversationIdAsync(int conversationId);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
        Task<bool> MarkMessageAsReadAsync(int messageId);
        Task<bool> MarkAllMessagesAsReadAsync(int conversationId, int userId);
        Task<int> GetUnreadMessageCountAsync(int conversationId, int userId);

        // Statistics
        Task<int> GetActiveConversationsCountAsync();
        Task<int> GetPendingConversationsCountAsync();
        Task<double> GetAverageResponseTimeAsync(DateTime? from = null, DateTime? to = null);
        Task<double> GetAverageResolutionTimeAsync(DateTime? from = null, DateTime? to = null);
        Task<double> GetCustomerSatisfactionScoreAsync(DateTime? from = null, DateTime? to = null);
        Task<Dictionary<string, int>> GetConversationsByStatusCountAsync();
        Task<Dictionary<string, int>> GetConversationsByCategoryCountAsync();

        // Agent Performance
        Task<IEnumerable<ChatConversation>> GetAgentConversationsAsync(int agentId, DateTime? from = null, DateTime? to = null);
        Task<int> GetAgentResolvedCountAsync(int agentId, DateTime? from = null, DateTime? to = null);
        Task<double> GetAgentAverageRatingAsync(int agentId, DateTime? from = null, DateTime? to = null);

        // Rating
        Task<bool> RateConversationAsync(int conversationId, int rating, string feedback);
    }
}
