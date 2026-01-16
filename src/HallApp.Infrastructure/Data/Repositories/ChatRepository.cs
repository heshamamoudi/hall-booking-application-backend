using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Chat Conversations
    /// </summary>
    public class ChatRepository : IChatRepository
    {
        private readonly DataContext _context;

        public ChatRepository(DataContext context)
        {
            _context = context;
        }

        #region Conversation CRUD

        public async Task<ChatConversation> GetConversationByIdAsync(int id)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Booking)
                .Include(c => c.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<ChatConversation>> GetAllConversationsAsync()
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByCustomerIdAsync(int customerId)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.CustomerId == customerId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByCreatedByUserIdAsync(int userId)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.CreatedByUserId == userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByConversationTypeAsync(string conversationType)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)  // AppUser entity - loads FirstName, LastName from Users table
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                    .ThenInclude(m => m.Sender)  // Load message sender for last message preview
                .Where(c => c.ConversationType == conversationType)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByAgentIdAsync(int agentId)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.CreatedBy)
                .Include(c => c.Hall)
                .Include(c => c.Vendor)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.SupportAgentId == agentId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByStatusAsync(string status)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetUnassignedConversationsAsync()
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.SupportAgentId == null && c.Status == "Open")
                .OrderByDescending(c => c.Priority == "Urgent" ? 4 : c.Priority == "High" ? 3 : c.Priority == "Normal" ? 2 : 1)
                .ThenBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatConversation>> GetConversationsByPriorityAsync(string priority)
        {
            return await _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Include(c => c.SupportAgent)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c => c.Priority == priority)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation)
        {
            await _context.ChatConversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<ChatConversation> UpdateConversationAsync(ChatConversation conversation)
        {
            _context.ChatConversations.Update(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<bool> DeleteConversationAsync(int id)
        {
            var conversation = await _context.ChatConversations.FindAsync(id);
            if (conversation == null) return false;

            _context.ChatConversations.Remove(conversation);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Message Operations

        public async Task<ChatMessage> GetMessageByIdAsync(int id)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesByConversationIdAsync(int conversationId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);

            // Update conversation's last message timestamp and total messages
            var conversation = await _context.ChatConversations.FindAsync(message.ConversationId);
            if (conversation != null)
            {
                conversation.LastMessageAt = message.SentAt;
                conversation.TotalMessages++;
            }

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllMessagesAsReadAsync(int conversationId, int userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(int conversationId, int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
                .CountAsync();
        }

        #endregion

        #region Statistics

        public async Task<int> GetActiveConversationsCountAsync()
        {
            return await _context.ChatConversations
                .Where(c => c.Status == "Open" || c.Status == "InProgress")
                .CountAsync();
        }

        public async Task<int> GetPendingConversationsCountAsync()
        {
            return await _context.ChatConversations
                .Where(c => c.Status == "Open" && c.SupportAgentId == null)
                .CountAsync();
        }

        public async Task<double> GetAverageResponseTimeAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations.AsQueryable();

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            var conversations = await query
                .Where(c => c.ResponseTime != null)
                .ToListAsync();

            if (!conversations.Any()) return 0;

            return conversations.Average(c => c.ResponseTime.Value.TotalMinutes);
        }

        public async Task<double> GetAverageResolutionTimeAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations.AsQueryable();

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            var conversations = await query
                .Where(c => c.ResolutionTime != null)
                .ToListAsync();

            if (!conversations.Any()) return 0;

            return conversations.Average(c => c.ResolutionTime.Value.TotalMinutes);
        }

        public async Task<double> GetCustomerSatisfactionScoreAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations.AsQueryable();

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            var ratings = await query
                .Where(c => c.CustomerRating != null)
                .Select(c => c.CustomerRating.Value)
                .ToListAsync();

            if (!ratings.Any()) return 0;

            return ratings.Average();
        }

        public async Task<Dictionary<string, int>> GetConversationsByStatusCountAsync()
        {
            return await _context.ChatConversations
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetConversationsByCategoryCountAsync()
        {
            return await _context.ChatConversations
                .GroupBy(c => c.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count);
        }

        #endregion

        #region Agent Performance

        public async Task<IEnumerable<ChatConversation>> GetAgentConversationsAsync(int agentId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations
                .Include(c => c.Customer)
                    .ThenInclude(cust => cust.AppUser)
                .Where(c => c.SupportAgentId == agentId);

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetAgentResolvedCountAsync(int agentId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations
                .Where(c => c.SupportAgentId == agentId && c.Status == "Resolved");

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            return await query.CountAsync();
        }

        public async Task<double> GetAgentAverageRatingAsync(int agentId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.ChatConversations
                .Where(c => c.SupportAgentId == agentId && c.CustomerRating != null);

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(c => c.CreatedAt <= to.Value);

            var ratings = await query
                .Select(c => c.CustomerRating.Value)
                .ToListAsync();

            if (!ratings.Any()) return 0;

            return ratings.Average();
        }

        #endregion

        #region Rating

        public async Task<bool> RateConversationAsync(int conversationId, int rating, string feedback)
        {
            var conversation = await _context.ChatConversations.FindAsync(conversationId);
            if (conversation == null) return false;

            conversation.CustomerRating = rating;
            conversation.CustomerFeedback = feedback ?? string.Empty;

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }
}
