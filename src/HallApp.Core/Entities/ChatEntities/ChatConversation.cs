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

    // Feedback
    public int? CustomerRating { get; set; }  // 1-5 stars

    [StringLength(1000)]
    public string CustomerFeedback { get; set; } = string.Empty;

    // Auto-close after 24h of inactivity
    public bool IsAutoCloseEnabled { get; set; } = true;

    // Navigation
    public List<ChatMessage> Messages { get; set; } = new();

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

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Soft delete for message recall
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // System message flag (auto-generated)
    public bool IsSystemMessage { get; set; } = false;
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
