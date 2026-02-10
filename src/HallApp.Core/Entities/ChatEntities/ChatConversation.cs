using System.ComponentModel.DataAnnotations;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Core.Entities.ChatEntities;

/// <summary>
/// Chat conversation for customer support
/// Purpose: Booking assistance and customer support only
/// </summary>
public class ChatConversation
{
    public int Id { get; set; }

    // Optional booking context
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    // Context for manager conversations
    public int? HallId { get; set; }  // For HallManager conversations
    public Hall? Hall { get; set; }
    
    public int? VendorId { get; set; }  // For VendorManager conversations
    public Vendor? Vendor { get; set; }

    // Participants
    public int? CustomerId { get; set; }  // Nullable - only for customer-initiated conversations
    public Customer? Customer { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }  // AppUser who created the conversation (Customer, HallManager, or VendorManager)
    public AppUser? CreatedBy { get; set; }

    [Required]
    [StringLength(50)]
    public string ConversationType { get; set; } = "Customer";  // Customer, HallManager, VendorManager - determines visibility

    public int? SupportAgentId { get; set; }  // Admin user handling the chat
    public AppUser? SupportAgent { get; set; }

    // Conversation details
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Open";  // Open, InProgress, Resolved, Closed

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = "General";  // General, Booking, Payment, Technical, Complaint

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = "Normal";  // Low, Normal, High, Urgent

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // Closed by user tracking
    public int? ClosedByUserId { get; set; }
    public AppUser? ClosedBy { get; set; }

    // Support Case link (optional - for case-initiated conversations)
    public int? CaseId { get; set; }
    public SupportCase? Case { get; set; }

    // Feedback (legacy - kept for backward compatibility)
    public int? CustomerRating { get; set; }  // 1-5 stars

    [StringLength(1000)]
    public string CustomerFeedback { get; set; } = string.Empty;

    // Auto-close after 24h of inactivity
    public bool IsAutoCloseEnabled { get; set; } = true;

    // Navigation
    public List<ChatMessage> Messages { get; set; } = new();

    // Ratings from participants (new per-user rating system)
    public List<ChatRating> Ratings { get; set; } = new();

    // Statistics
    public int TotalMessages { get; set; } = 0;
    public TimeSpan? ResponseTime { get; set; }  // Time to first agent response
    public TimeSpan? ResolutionTime { get; set; }  // Time to resolve
}

/// <summary>
/// Individual chat message
/// </summary>
public class ChatMessage
{
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    [Required]
    public int SenderId { get; set; }
    public AppUser? Sender { get; set; }

    [Required]
    [StringLength(20)]
    public string SenderType { get; set; } = string.Empty;  // Customer, Admin

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string MessageType { get; set; } = "Text";  // Text, Image, File, System

    [StringLength(500)]
    public string? AttachmentUrl { get; set; }

    [StringLength(100)]
    public string? AttachmentName { get; set; }

    public long? AttachmentSize { get; set; }

    // DEPRECATED: Use ChatMessageReadStatus for per-user read tracking
    // Kept for backward compatibility - represents if ANY user has read
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Soft delete for message recall
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // System message flag (auto-generated)
    public bool IsSystemMessage { get; set; } = false;

    // Per-user read status tracking
    public List<ChatMessageReadStatus> ReadStatuses { get; set; } = [];
}

/// <summary>
/// Tracks per-user read status for chat messages.
/// This enables proper unread message tracking where each user sees their own read/unread status.
/// </summary>
public class ChatMessageReadStatus
{
    public int Id { get; set; }

    [Required]
    public int MessageId { get; set; }
    public ChatMessage? Message { get; set; }

    [Required]
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Chat statistics and analytics
/// </summary>
public class ChatStatistics
{
    public int Id { get; set; }
    public DateTime Date { get; set; }

    public int TotalConversations { get; set; }
    public int OpenConversations { get; set; }
    public int ResolvedConversations { get; set; }
    public int ClosedConversations { get; set; }

    public double AverageResponseTime { get; set; }  // in minutes
    public double AverageResolutionTime { get; set; }  // in minutes
    public double CustomerSatisfactionScore { get; set; }  // average rating

    public int TotalMessages { get; set; }
    public int MessagesFromCustomers { get; set; }
    public int MessagesFromAgents { get; set; }
}

/// <summary>
/// Per-user rating for a chat conversation.
/// Allows any participant (Customer, HallManager, VendorManager) to rate the conversation.
/// One rating per user per conversation (enforced by unique constraint).
/// </summary>
public class ChatRating
{
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    [Required]
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    /// <summary>
    /// Rating value from 1 to 5 stars
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Optional comment about the experience (sanitized for XSS)
    /// </summary>
    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Support case/ticket that initiates a chat conversation.
/// Cases provide structured context for support interactions.
/// </summary>
public class SupportCase
{
    public int Id { get; set; }

    /// <summary>
    /// Human-readable case number (e.g., "CASE-2026-00001")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CaseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the issue
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
    /// Status: Open, InProgress, Resolved, Closed
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Open";

    /// <summary>
    /// User who created the case
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }
    public AppUser? CreatedBy { get; set; }

    /// <summary>
    /// Optional: Related booking for context
    /// </summary>
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>
    /// Optional: Related hall for context
    /// </summary>
    public int? HallId { get; set; }
    public Hall? Hall { get; set; }

    /// <summary>
    /// Optional: Related vendor for context
    /// </summary>
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    /// <summary>
    /// The chat conversation associated with this case (1:1 relationship)
    /// </summary>
    public int? ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
