using HallApp.Core.Entities.ChatEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface IChatService
    {
        // Conversation Management
        Task<ChatConversation> GetConversationByIdAsync(int id);
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
        Task<ChatConversation> CloseConversationAsync(int conversationId, string resolutionNotes);
        Task<ChatConversation> ReopenConversationAsync(int conversationId);
        Task<ChatConversation> TransferConversationAsync(int conversationId, int newAgentId);

        // Message Operations
        Task<ChatMessage> SendMessageAsync(int conversationId, int senderId, string message, string senderType);
        Task<IEnumerable<ChatMessage>> GetConversationMessagesAsync(int conversationId);
        Task<bool> MarkMessagesAsReadAsync(int conversationId, int userId);
        Task<int> GetUnreadCountAsync(int conversationId, int userId);

        // Rating
        Task<bool> RateConversationAsync(int conversationId, int rating, string feedback);

        // Statistics
        Task<object> GetChatStatisticsAsync(DateTime? from = null, DateTime? to = null);
        Task<object> GetAgentPerformanceAsync(int agentId, DateTime? from = null, DateTime? to = null);
    }
}
