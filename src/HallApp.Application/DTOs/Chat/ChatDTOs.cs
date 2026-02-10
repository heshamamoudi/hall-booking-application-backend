using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Chat
{
    /// <summary>
    /// Chat conversation DTO - Clean design with no nulls
    /// </summary>
    public class ChatConversationDto
    {
        public int Id { get; set; }
        
        // Context IDs (0 if not applicable)
        public int BookingId { get; set; }
        public int HallId { get; set; }
        public int VendorId { get; set; }
        public int CustomerId { get; set; }
        
        // Creator information
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string CreatedByEmail { get; set; } = string.Empty;
        
        // Context names (for display)
        public string HallName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        
        // Support agent
        public int SupportAgentId { get; set; }
        public string SupportAgentName { get; set; } = string.Empty;
        
        // Conversation details
        public string Subject { get; set; } = string.Empty;
        public string ConversationType { get; set; } = string.Empty;  // Customer, HallManager, VendorManager
        public string Status { get; set; } = string.Empty;  // Open, InProgress, Resolved, Closed
        public string Category { get; set; } = string.Empty;  // General, Booking, Payment, Technical, Complaint
        public string Priority { get; set; } = string.Empty;  // Low, Normal, High, Urgent
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime ClaimedAt { get; set; }
        public DateTime ResolvedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        
        // Feedback
        public int CustomerRating { get; set; }  // 0 if not rated, 1-5 if rated
        public string CustomerFeedback { get; set; } = string.Empty;
        
        // Statistics
        public int TotalMessages { get; set; }
        public int UnreadCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public double ResponseTimeMinutes { get; set; }  // 0 if not responded yet
        public double ResolutionTimeMinutes { get; set; }  // 0 if not resolved yet
        
        // Legacy compatibility - maps to CreatedByName
        public string CustomerName => CreatedByName;
        public string CustomerEmail => CreatedByEmail;
    }

    /// <summary>
    /// Chat message DTO
    /// </summary>
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public string AttachmentUrl { get; set; } = string.Empty;
        public string AttachmentName { get; set; } = string.Empty;
        public long? AttachmentSize { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSystemMessage { get; set; }
    }

    /// <summary>
    /// Create conversation DTO
    /// </summary>
    public class CreateChatConversationDto
    {
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = "General";

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Normal";

        [StringLength(2000)]
        public string InitialMessage { get; set; } = string.Empty;

        public int? BookingId { get; set; }
        public int? HallId { get; set; }  // For HallManager conversations
        public int? VendorId { get; set; }  // For VendorManager conversations
    }

    /// <summary>
    /// Send message DTO
    /// </summary>
    public class SendMessageDto
    {
        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Assign conversation DTO
    /// </summary>
    public class AssignConversationDto
    {
        public int? AgentId { get; set; } // If null, assign to current user
    }

    /// <summary>
    /// Close conversation DTO
    /// </summary>
    public class CloseConversationDto
    {
        [StringLength(2000)]
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transfer conversation DTO
    /// </summary>
    public class TransferConversationDto
    {
        [Required]
        public int NewAgentId { get; set; }
    }

    /// <summary>
    /// Rate conversation DTO (input)
    /// </summary>
    public class RateConversationDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Feedback { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chat rating response DTO (output)
    /// </summary>
    public class ChatRatingResponseDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime RatedAt { get; set; }
    }

    #region Support Case DTOs

    /// <summary>
    /// Create support case DTO (input)
    /// Creates a new support case which automatically generates a chat conversation.
    /// </summary>
    public class CreateSupportCaseDto
    {
        /// <summary>
        /// Brief subject/title of the case
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the issue
        /// </summary>
        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category: General, Booking, Payment, Technical, Complaint, Account, Other
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "General";

        /// <summary>
        /// Priority: Low, Normal, High, Urgent
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Optional: Related booking ID for context
        /// </summary>
        public int? BookingId { get; set; }

        /// <summary>
        /// Optional: Related hall ID for HallManager cases
        /// </summary>
        public int? HallId { get; set; }

        /// <summary>
        /// Optional: Related vendor ID for VendorManager cases
        /// </summary>
        public int? VendorId { get; set; }
    }

    /// <summary>
    /// Support case response DTO (output)
    /// </summary>
    public class SupportCaseDto
    {
        public int Id { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Creator info
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        // Context (0 if not applicable)
        public int BookingId { get; set; }
        public int HallId { get; set; }
        public int VendorId { get; set; }
        public string HallName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        // Associated conversation
        public int ConversationId { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    /// <summary>
    /// Support case with conversation details (output)
    /// </summary>
    public class SupportCaseWithConversationDto : SupportCaseDto
    {
        public ChatConversationDto? Conversation { get; set; }
    }

    #endregion

    /// <summary>
    /// Updated ChatConversationDto with new fields
    /// </summary>
    public class ChatConversationExtendedDto : ChatConversationDto
    {
        // Closed by info
        public int ClosedByUserId { get; set; }
        public string ClosedByName { get; set; } = string.Empty;

        // Support Case link
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;

        // Ratings summary
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }

        // User's own rating (if they have rated)
        public ChatRatingResponseDto? UserRating { get; set; }

        // Flag to indicate if conversation can be rated by current user
        public bool CanRate { get; set; }
    }
}
