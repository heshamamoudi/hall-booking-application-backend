using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatHubService _chatHubService;
        private readonly ILogger<ChatService> _logger;
        private readonly HtmlSanitizerService _htmlSanitizerService;
        private readonly IHallManagerService _hallManagerService;
        private readonly IVendorManagerService _vendorManagerService;

        public ChatService(
            IUnitOfWork unitOfWork,
            IChatHubService chatHubService,
            ILogger<ChatService> logger,
            HtmlSanitizerService htmlSanitizerService,
            IHallManagerService hallManagerService,
            IVendorManagerService vendorManagerService)
        {
            _unitOfWork = unitOfWork;
            _chatHubService = chatHubService;
            _logger = logger;
            _htmlSanitizerService = htmlSanitizerService;
            _hallManagerService = hallManagerService;
            _vendorManagerService = vendorManagerService;
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
            // SECURITY: Sanitize message content to prevent XSS attacks (CHAT-RISK-001)
            var sanitizedMessage = _htmlSanitizerService.Sanitize(message);

            if (_htmlSanitizerService.IsPotentialXss(message))
            {
                _logger.LogWarning("Potential XSS attack detected in message from user {SenderId} in conversation {ConversationId}", senderId, conversationId);
            }

            var chatMessage = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                SenderType = senderType,
                Message = sanitizedMessage,
                MessageType = "Text",
                SentAt = DateTime.UtcNow,
                IsRead = false,
                IsSystemMessage = false
            };

            var createdMessage = await _unitOfWork.ChatRepository.AddMessageAsync(chatMessage);

            // FIX CHAT-BUG-001: Create per-user read status for sender (sender's own message is always "read" for them)
            await _unitOfWork.ChatRepository.CreateSenderReadStatusAsync(createdMessage.Id, senderId);

            // FIXED: Load Sender navigation property for SignalR
            var messageWithSender = await _unitOfWork.ChatRepository.GetMessageByIdAsync(createdMessage.Id);

            // Update conversation's last message time and count
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.TotalMessages += 1;
                await _unitOfWork.ChatRepository.UpdateConversationAsync(conversation);
            }

            // Send real-time message via SignalR
            try
            {
                // FIXED: Send message WITH Sender loaded for SenderName
                await _chatHubService.SendMessageToConversationAsync(conversationId, messageWithSender);
                _logger.LogInformation("Real-time message sent to conversation {ConversationId}", conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send real-time message for conversation {ConversationId}", conversationId);
            }

            return createdMessage; // Return original for API response
        }

        public async Task<IEnumerable<ChatMessage>> GetConversationMessagesAsync(int conversationId)
        {
            return await _unitOfWork.ChatRepository.GetMessagesByConversationIdAsync(conversationId);
        }

        public async Task<bool> MarkMessagesAsReadAsync(int conversationId, int userId)
        {
            // FIX CHAT-BUG-001: Use per-user read status tracking instead of global IsRead flag
            var result = await _unitOfWork.ChatRepository.MarkMessagesAsReadForUserAsync(conversationId, userId);

            // Send real-time read notification via SignalR
            if (result)
            {
                try
                {
                    await _chatHubService.SendMessageReadNotificationAsync(conversationId, userId);
                    _logger.LogInformation("Messages marked as read in conversation {ConversationId} by user {UserId}", conversationId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send read notification for conversation {ConversationId}", conversationId);
                }
            }

            return result;
        }

        public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
        {
            // FIX CHAT-BUG-001: Use per-user read status tracking instead of global IsRead flag
            return await _unitOfWork.ChatRepository.GetUnreadMessageCountForUserAsync(conversationId, userId);
        }

        public async Task<bool> MarkMessagesAsUnreadAsync(int conversationId, int userId)
        {
            // Use per-user read status tracking to mark messages as unread
            var result = await _unitOfWork.ChatRepository.MarkMessagesAsUnreadForUserAsync(conversationId, userId);

            if (result)
            {
                _logger.LogInformation("Messages marked as unread in conversation {ConversationId} by user {UserId}", conversationId, userId);
            }

            return result;
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Check if a user has access to a specific conversation.
        /// Used by ChatHub for SignalR group authorization (CHAT-RISK-002).
        /// Mirrors the authorization logic from ChatController.CanAccessConversationAsync.
        /// </summary>
        public async Task<bool> CanAccessConversationAsync(int userId, int conversationId, IEnumerable<string> userRoles)
        {
            var conversation = await _unitOfWork.ChatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                _logger.LogWarning("CanAccessConversationAsync: Conversation {ConversationId} not found", conversationId);
                return false;
            }

            var roles = userRoles?.ToList() ?? [];

            // Admin has access to all conversations
            if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                return true;

            // Customer who created the conversation
            if (conversation.CreatedByUserId == userId)
                return true;

            // Customer (by CustomerId)
            if (conversation.CustomerId == userId)
                return true;

            // Support agent assigned to the conversation
            if (conversation.SupportAgentId == userId)
                return true;

            // HallManager - check if they manage the hall associated with the conversation
            if (roles.Contains("HallManager", StringComparer.OrdinalIgnoreCase) && conversation.HallId.HasValue)
            {
                var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                if (hallManager?.Halls?.Any(h => h.ID == conversation.HallId.Value) == true)
                    return true;
            }

            // VendorManager - check if they manage the vendor associated with the conversation
            if (roles.Contains("VendorManager", StringComparer.OrdinalIgnoreCase) && conversation.VendorId.HasValue)
            {
                var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                if (vendorManager?.Vendors?.Any(v => v.Id == conversation.VendorId.Value) == true)
                    return true;
            }

            _logger.LogWarning("CanAccessConversationAsync: User {UserId} denied access to conversation {ConversationId}", userId, conversationId);
            return false;
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
