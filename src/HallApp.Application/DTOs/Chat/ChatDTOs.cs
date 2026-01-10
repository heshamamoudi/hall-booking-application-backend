using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Chat
{
    /// <summary>
    /// Chat conversation DTO
    /// </summary>
    public class ChatConversationDto
    {
        public int Id { get; set; }
        public int? BookingId { get; set; }
        public int? HallId { get; set; }
        public int? VendorId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int? SupportAgentId { get; set; }
        public string SupportAgentName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ConversationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int? CustomerRating { get; set; }
        public string CustomerFeedback { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int UnreadCount { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public double? ResponseTimeMinutes { get; set; }
        public double? ResolutionTimeMinutes { get; set; }
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
    /// Rate conversation DTO
    /// </summary>
    public class RateConversationDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Feedback { get; set; } = string.Empty;
    }
}
