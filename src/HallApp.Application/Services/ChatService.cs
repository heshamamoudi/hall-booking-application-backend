using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;

namespace HallApp.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Conversation Management

        public async Task<ChatConversation> GetConversationByIdAsync(int id)
        {
            return await _unitOfWork.ChatRepository.GetConversationByIdAsync(id);
        }

        public async Task<IEnumerable<ChatConversation>> GetAllConversationsAsync()
        {
            return await _unitOfWork.ChatRepository.GetAllConversationsAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetCustomerConversationsAsync(int customerId)
        {
            return await _unitOfWork.ChatRepository.GetConversationsByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByCreatedByUserIdAsync(int userId)
        {
            return await _unitOfWork.ChatRepository.GetConversationsByCreatedByUserIdAsync(userId);
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByConversationTypeAsync(string conversationType)
        {
            return await _unitOfWork.ChatRepository.GetConversationsByConversationTypeAsync(conversationType);
        }

        public async Task<IEnumerable<ChatConversation>> GetAgentConversationsAsync(int agentId)
        {
            return await _unitOfWork.ChatRepository.GetConversationsByAgentIdAsync(agentId);
        }

        public async Task<IEnumerable<ChatConversation>> GetUnassignedConversationsAsync()
        {
            return await _unitOfWork.ChatRepository.GetUnassignedConversationsAsync();
        }

        public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation)
        {
            // Set initial values
            conversation.CreatedAt = DateTime.UtcNow;
            conversation.Status = "Open";
            conversation.TotalMessages = 0;

            // Create system message
            var systemMessage = new ChatMessage
            {
                SenderId = conversation.CreatedByUserId,
                SenderType = "System",
                Message = $"Conversation started: {conversation.Subject}",
                MessageType = "System",
                IsSystemMessage = true,
                SentAt = DateTime.UtcNow,
                ConversationId = conversation.Id
            };

            var createdConversation = await _unitOfWork.ChatRepository.CreateConversationAsync(conversation);

            // Add system message
            systemMessage.ConversationId = createdConversation.Id;
            await _unitOfWork.ChatRepository.AddMessageAsync(systemMessage);

            return createdConversation;
        }

        public async Task<ChatConversation> UpdateConversationAsync(ChatConversation conversation)
        {
            return await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
        }

        public async Task<bool> DeleteConversationAsync(int id)
        {
            return await _unitOfWork.ChatRepository.DeleteConversationAsync(id);
        }

        #endregion

        #region Conversation Actions

        public async Task<ChatConversation> AssignConversationAsync(int conversationId, int agentId)
        {
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            conversation.SupportAgentId = agentId;
            conversation.ClaimedAt = DateTime.UtcNow;
            conversation.Status = "InProgress";

            // Add system message
            var systemMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = agentId,
                SenderType = "System",
                Message = "Support agent has joined the conversation",
                MessageType = "System",
                IsSystemMessage = true,
                SentAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatRepository.AddMessageAsync(systemMessage);

            // Calculate response time if this is the first agent response
            if (conversation.ResponseTime == null)
            {
                conversation.ResponseTime = DateTime.UtcNow - conversation.CreatedAt;
            }

            return await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
        }

        public async Task<ChatConversation> CloseConversationAsync(int conversationId, string resolutionNotes)
        {
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            conversation.Status = "Resolved";
            conversation.ResolvedAt = DateTime.UtcNow;
            conversation.ClosedAt = DateTime.UtcNow;
            conversation.ResolutionTime = DateTime.UtcNow - conversation.CreatedAt;

            // Add system message
            var systemMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = conversation.SupportAgentId ?? 0,
                SenderType = "System",
                Message = "Conversation has been resolved and closed",
                MessageType = "System",
                IsSystemMessage = true,
                SentAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatRepository.AddMessageAsync(systemMessage);

            return await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
        }

        public async Task<ChatConversation> ReopenConversationAsync(int conversationId)
        {
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            conversation.Status = "Open";
            conversation.ResolvedAt = null;
            conversation.ClosedAt = null;

            // Add system message
            var systemMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = conversation.CreatedByUserId,
                SenderType = "System",
                Message = "Conversation has been reopened",
                MessageType = "System",
                IsSystemMessage = true,
                SentAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatRepository.AddMessageAsync(systemMessage);

            return await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
        }

        public async Task<ChatConversation> TransferConversationAsync(int conversationId, int newAgentId)
        {
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found");

            var oldAgentId = conversation.SupportAgentId;
            conversation.SupportAgentId = newAgentId;
            conversation.Status = "InProgress";

            // Add system message
            var systemMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = newAgentId,
                SenderType = "System",
                Message = "Conversation has been transferred to another agent",
                MessageType = "System",
                IsSystemMessage = true,
                SentAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatRepository.AddMessageAsync(systemMessage);

            return await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
        }

        #endregion

        #region Message Operations

        public async Task<ChatMessage> SendMessageAsync(int conversationId, int senderId, string message, string senderType)
        {
            var chatMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                SenderType = senderType,
                Message = message,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                IsRead = false,
                IsSystemMessage = false
            };

            return await _unitOfWork.ChatRepository.AddMessageAsync(chatMessage);
        }

        public async Task<IEnumerable<ChatMessage>> GetConversationMessagesAsync(int conversationId)
        {
            return await _unitOfWork.ChatRepository.GetMessagesByConversationIdAsync(conversationId);
        }

        public async Task<bool> MarkMessagesAsReadAsync(int conversationId, int userId)
        {
            return await _unitOfWork.ChatRepository.MarkAllMessagesAsReadAsync(conversationId, userId);
        }

        public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
        {
            return await _unitOfWork.ChatRepository.GetUnreadMessageCountAsync(conversationId, userId);
        }

        #endregion

        #region Rating

        public async Task<bool> RateConversationAsync(int conversationId, int rating, string feedback)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");

            return await _unitOfWork.ChatRepository.RateConversationAsync(conversationId, rating, feedback);
        }

        #endregion

        #region Statistics

        public async Task<object> GetChatStatisticsAsync(DateTime? from = null, DateTime? to = null)
        {
            var activeCount = await _unitOfWork.ChatRepository.GetActiveConversationsCountAsync();
            var pendingCount = await _unitOfWork.ChatRepository.GetPendingConversationsCountAsync();
            var avgResponseTime = await _unitOfWork.ChatRepository.GetAverageResponseTimeAsync(from, to);
            var avgResolutionTime = await _unitOfWork.ChatRepository.GetAverageResolutionTimeAsync(from, to);
            var satisfactionScore = await _unitOfWork.ChatRepository.GetCustomerSatisfactionScoreAsync(from, to);
            var statusCounts = await _unitOfWork.ChatRepository.GetConversationsByStatusCountAsync();
            var categoryCounts = await _unitOfWork.ChatRepository.GetConversationsByCategoryCountAsync();

            return new
            {
                activeConversations = activeCount,
                pendingConversations = pendingCount,
                averageResponseTimeMinutes = Math.Round(avgResponseTime, 2),
                averageResolutionTimeMinutes = Math.Round(avgResolutionTime, 2),
                customerSatisfactionScore = Math.Round(satisfactionScore, 2),
                conversationsByStatus = statusCounts,
                conversationsByCategory = categoryCounts,
                period = new
                {
                    from = from?.ToString("yyyy-MM-dd"),
                    to = to?.ToString("yyyy-MM-dd")
                }
            };
        }

        public async Task<object> GetAgentPerformanceAsync(int agentId, DateTime? from = null, DateTime? to = null)
        {
            var conversations = await _unitOfWork.ChatRepository.GetAgentConversationsAsync(agentId, from, to);
            var resolvedCount = await _unitOfWork.ChatRepository.GetAgentResolvedCountAsync(agentId, from, to);
            var averageRating = await _unitOfWork.ChatRepository.GetAgentAverageRatingAsync(agentId, from, to);

            return new
            {
                agentId,
                totalConversations = conversations.Count(),
                resolvedConversations = resolvedCount,
                averageRating = Math.Round(averageRating, 2),
                period = new
                {
                    from = from?.ToString("yyyy-MM-dd"),
                    to = to?.ToString("yyyy-MM-dd")
                }
            };
        }

        #endregion
    }
}
