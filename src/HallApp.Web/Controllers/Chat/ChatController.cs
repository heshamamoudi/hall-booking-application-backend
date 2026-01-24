using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Chat;
using HallApp.Core.Entities.ChatEntities;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace HallApp.Web.Controllers.Chat
{
    /// <summary>
    /// Chat and Customer Support Controller
    /// Manages conversations, messages, and employee ratings
    /// </summary>
    [Authorize]
    [Route("api/chat")]
    public class ChatController : BaseApiController
    {
        private readonly IChatService _chatService;
        private readonly IMapper _mapper;
        private readonly IHallManagerService _hallManagerService;
        private readonly IVendorManagerService _vendorManagerService;
        private readonly IBookingService _bookingService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService, 
            IMapper mapper, 
            IHallManagerService hallManagerService, 
            IVendorManagerService vendorManagerService, 
            IBookingService bookingService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _mapper = mapper;
            _hallManagerService = hallManagerService;
            _vendorManagerService = vendorManagerService;
            _bookingService = bookingService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        #region Manager Assignments

        /// <summary>
        /// Get current manager's assigned halls (for dropdown in conversation creation)
        /// </summary>
        [HttpGet("manager/halls")]
        [Authorize(Roles = "HallManager")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetManagerHalls()
        {
            try
            {
                var userId = GetCurrentUserId();
                var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                
                if (hallManager == null || hallManager.Halls == null || !hallManager.Halls.Any())
                {
                    return Success<IEnumerable<object>>(new List<object>(), "No halls assigned");
                }

                var halls = hallManager.Halls.Select(h => new
                {
                    id = h.ID,
                    name = h.Name,
                    // Add city if available from hall entity
                    displayName = h.Name
                });

                return Success<IEnumerable<object>>(halls, "Halls retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<object>>($"Error retrieving halls: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get current manager's assigned vendors (for dropdown in conversation creation)
        /// </summary>
        [HttpGet("manager/vendors")]
        [Authorize(Roles = "VendorManager")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetManagerVendors()
        {
            try
            {
                var userId = GetCurrentUserId();
                var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                
                if (vendorManager == null || vendorManager.Vendors == null || !vendorManager.Vendors.Any())
                {
                    return Success<IEnumerable<object>>(new List<object>(), "No vendors assigned");
                }

                var vendors = vendorManager.Vendors.Select(v => new
                {
                    id = v.Id,
                    name = v.Name,
                    displayName = v.Name
                });

                return Success<IEnumerable<object>>(vendors, "Vendors retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<object>>($"Error retrieving vendors: {ex.Message}", 500);
            }
        }

        #endregion

        #region Conversation Management

        /// <summary>
        /// Get active conversations (role-based filtering)
        /// Admin: sees all active
        /// Hall/Vendor Managers: see only their related active chats
        /// Customer: see only their own active chats
        /// </summary>
        [HttpGet("conversations/active")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetActiveConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                IEnumerable<ChatConversation> conversations;

                if (User.IsInRole("Admin"))
                {
                    // Admin sees all active conversations
                    conversations = await _chatService.GetAllConversationsAsync();
                    conversations = conversations.Where(c => c.Status == "Open" || c.Status == "InProgress");
                }
                else if (User.IsInRole("HallManager"))
                {
                    // Hall Managers see conversations for their assigned halls
                    var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                    if (hallManager != null)
                    {
                        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
                        _logger.LogDebug("Hall Manager {UserId} - Assigned Hall IDs: [{HallIds}]", userId, string.Join(", ", hallIds));

                        var allConversations = await _chatService.GetConversationsByConversationTypeAsync("HallManager");
                        _logger.LogDebug("Total HallManager conversations: {Count}", allConversations.Count());

                        foreach (var conv in allConversations)
                        {
                            var passes = conv.HallId.HasValue && hallIds.Contains(conv.HallId.Value) && (conv.Status == "Open" || conv.Status == "InProgress");
                            _logger.LogTrace("Conv {ConvId}: HallId={HallId}, Status={Status}, PassesFilter={Passes}", conv.Id, conv.HallId, conv.Status, passes);
                        }

                        conversations = allConversations.Where(c =>
                            c.HallId.HasValue &&
                            hallIds.Contains(c.HallId.Value) &&
                            (c.Status == "Open" || c.Status == "InProgress"));

                        _logger.LogDebug("Filtered conversations for Hall Manager: {Count}", conversations.Count());
                    }
                    else
                    {
                        _logger.LogWarning("Hall Manager not found for userId {UserId}", userId);
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("VendorManager"))
                {
                    // Vendor Managers see conversations for their assigned vendors
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                    if (vendorManager != null)
                    {
                        var vendorIds = vendorManager.Vendors.Select(v => v.Id).ToList();
                        conversations = await _chatService.GetConversationsByConversationTypeAsync("VendorManager");
                        conversations = conversations.Where(c => c.VendorId.HasValue && vendorIds.Contains(c.VendorId.Value) && (c.Status == "Open" || c.Status == "InProgress"));
                    }
                    else
                    {
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("Customer"))
                {
                    // Customers see their own active conversations
                    conversations = await _chatService.GetCustomerConversationsAsync(userId);
                    conversations = conversations.Where(c => c.Status == "Open" || c.Status == "InProgress");
                }
                else
                {
                    // Default: empty list
                    conversations = new List<ChatConversation>();
                }

                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Active conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve active conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get historical conversations (role-based filtering)
        /// Admin: sees all historical
        /// Hall/Vendor Managers: see only their related historical chats
        /// Customer: see only their own historical chats
        /// </summary>
        [HttpGet("conversations/historical")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetHistoricalConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                IEnumerable<ChatConversation> conversations;

                if (User.IsInRole("Admin"))
                {
                    // Admin sees all historical conversations
                    conversations = await _chatService.GetAllConversationsAsync();
                    conversations = conversations.Where(c => c.Status == "Resolved" || c.Status == "Closed");
                }
                else if (User.IsInRole("HallManager"))
                {
                    // Hall Managers see historical conversations for their assigned halls
                    var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                    if (hallManager != null)
                    {
                        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
                        conversations = await _chatService.GetConversationsByConversationTypeAsync("HallManager");
                        conversations = conversations.Where(c => c.HallId.HasValue && hallIds.Contains(c.HallId.Value) && (c.Status == "Resolved" || c.Status == "Closed"));
                    }
                    else
                    {
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("VendorManager"))
                {
                    // Vendor Managers see historical conversations for their assigned vendors
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                    if (vendorManager != null)
                    {
                        var vendorIds = vendorManager.Vendors.Select(v => v.Id).ToList();
                        conversations = await _chatService.GetConversationsByConversationTypeAsync("VendorManager");
                        conversations = conversations.Where(c => c.VendorId.HasValue && vendorIds.Contains(c.VendorId.Value) && (c.Status == "Resolved" || c.Status == "Closed"));
                    }
                    else
                    {
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("Customer"))
                {
                    // Customers see their own historical conversations
                    conversations = await _chatService.GetCustomerConversationsAsync(userId);
                    conversations = conversations.Where(c => c.Status == "Resolved" || c.Status == "Closed");
                }
                else
                {
                    // Default: empty list
                    conversations = new List<ChatConversation>();
                }

                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Historical conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve historical conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get unread message count for current user
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                IEnumerable<ChatConversation> conversations;

                if (User.IsInRole("Admin"))
                {
                    conversations = await _chatService.GetAllConversationsAsync();
                }
                else if (User.IsInRole("HallManager"))
                {
                    var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                    if (hallManager != null)
                    {
                        var hallIds = hallManager.Halls.Select(h => h.ID).ToList();
                        conversations = await _chatService.GetConversationsByConversationTypeAsync("HallManager");
                        conversations = conversations.Where(c => c.HallId.HasValue && hallIds.Contains(c.HallId.Value));
                    }
                    else
                    {
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("VendorManager"))
                {
                    var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                    if (vendorManager != null)
                    {
                        var vendorIds = vendorManager.Vendors.Select(v => v.Id).ToList();
                        conversations = await _chatService.GetConversationsByConversationTypeAsync("VendorManager");
                        conversations = conversations.Where(c => c.VendorId.HasValue && vendorIds.Contains(c.VendorId.Value));
                    }
                    else
                    {
                        conversations = new List<ChatConversation>();
                    }
                }
                else if (User.IsInRole("Customer"))
                {
                    conversations = await _chatService.GetCustomerConversationsAsync(userId);
                }
                else
                {
                    conversations = new List<ChatConversation>();
                }

                // Count unread messages across all conversations
                int unreadCount = 0;
                foreach (var conversation in conversations)
                {
                    unreadCount += await _chatService.GetUnreadCountAsync(conversation.Id, userId);
                }

                return Success(unreadCount, "Unread count retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<int>($"Failed to retrieve unread count: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all conversations (Admin/Support only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("conversations")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetAllConversations()
        {
            try
            {
                var conversations = await _chatService.GetAllConversationsAsync();
                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get conversation by ID
        /// </summary>
        [HttpGet("conversations/{id:int}")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> GetConversationById(int id)
        {
            try
            {
                var conversation = await _chatService.GetConversationByIdAsync(id);
                if (conversation == null)
                {
                    return Error<ChatConversationDto>("Conversation not found", 404);
                }

                // Authorization: Only allow customer, assigned agent, or admin
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin && conversation.CustomerId != userId && conversation.SupportAgentId != userId)
                {
                    return Error<ChatConversationDto>("Access denied", 403);
                }

                var conversationDto = _mapper.Map<ChatConversationDto>(conversation);
                return Success(conversationDto, "Conversation retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to retrieve conversation: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get my conversations (Customer view)
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet("my-conversations")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetMyConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversations = await _chatService.GetCustomerConversationsAsync(userId);
                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Your conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get assigned conversations (Agent view)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("assigned-conversations")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetAssignedConversations()
        {
            try
            {
                var userId = GetCurrentUserId();
                var conversations = await _chatService.GetAgentConversationsAsync(userId);
                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Assigned conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get unassigned conversations waiting for support (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("unassigned-conversations")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatConversationDto>>>> GetUnassignedConversations()
        {
            try
            {
                var conversations = await _chatService.GetUnassignedConversationsAsync();
                var conversationDtos = _mapper.Map<IEnumerable<ChatConversationDto>>(conversations);
                return Success(conversationDtos, "Unassigned conversations retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatConversationDto>>($"Failed to retrieve conversations: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Create new conversation (Customers, Managers and Admins can create support cases)
        /// </summary>
        [Authorize(Roles = "Customer,HallManager,VendorManager,Admin")]
        [HttpPost("conversations")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> CreateConversation([FromBody] CreateChatConversationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<ChatConversationDto>($"Invalid data: {errors}", 400);
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation("Creating conversation for user {UserId}, Roles: {Roles}", userId, string.Join(",", User.Claims.Where(c => c.Type.Contains("role")).Select(c => c.Value)));
                var conversation = _mapper.Map<ChatConversation>(dto);
                conversation.CreatedByUserId = userId;

                // If bookingId is provided, fetch the booking to get HallId
                if (dto.BookingId.HasValue)
                {
                    var booking = await _bookingService.GetBookingByIdAsync(dto.BookingId.Value);
                    if (booking != null)
                    {
                        conversation.HallId = booking.HallId;
                    }
                }

                // Determine sender type and conversation type based on user role
                string senderType;
                if (User.IsInRole("Customer"))
                {
                    conversation.CustomerId = userId;  // For customers, set CustomerId
                    conversation.ConversationType = "Customer";
                    senderType = "Customer";
                }
                else if (User.IsInRole("HallManager"))
                {
                    conversation.ConversationType = "HallManager";
                    
                    // Smart assignment: Priority order
                    // 1. From booking (already set above)
                    // 2. From DTO (user selected)
                    // 3. Auto-assign if manager has only one hall
                    if (!conversation.HallId.HasValue)
                    {
                        if (dto.HallId.HasValue)
                        {
                            conversation.HallId = dto.HallId;
                        }
                        else
                        {
                            // Auto-assign if manager has only one hall
                            var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userId);
                            if (hallManager != null && hallManager.Halls.Count == 1)
                            {
                                conversation.HallId = hallManager.Halls.First().ID;
                            }
                            else if (hallManager == null || hallManager.Halls.Count == 0)
                            {
                                return Error<ChatConversationDto>("Hall Manager has no assigned halls", 400);
                            }
                            else
                            {
                                return Error<ChatConversationDto>("Hall Manager has multiple halls assigned. Please specify HallId.", 400);
                            }
                        }
                    }
                    senderType = "HallManager";
                }
                else if (User.IsInRole("VendorManager"))
                {
                    conversation.ConversationType = "VendorManager";
                    
                    // Smart assignment: Priority order
                    // 1. From DTO (user selected)
                    // 2. Auto-assign if manager has only one vendor
                    if (!conversation.VendorId.HasValue)
                    {
                        if (dto.VendorId.HasValue)
                        {
                            conversation.VendorId = dto.VendorId;
                        }
                        else
                        {
                            // Auto-assign if manager has only one vendor
                            var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userId);
                            if (vendorManager != null && vendorManager.Vendors.Count == 1)
                            {
                                conversation.VendorId = vendorManager.Vendors.First().Id;
                            }
                            else if (vendorManager == null || vendorManager.Vendors.Count == 0)
                            {
                                return Error<ChatConversationDto>("Vendor Manager has no assigned vendors", 400);
                            }
                            else
                            {
                                return Error<ChatConversationDto>("Vendor Manager has multiple vendors assigned. Please specify VendorId.", 400);
                            }
                        }
                    }
                    senderType = "VendorManager";
                }
                else
                {
                    conversation.ConversationType = "Customer";
                    senderType = "User";
                }

                var createdConversation = await _chatService.CreateConversationAsync(conversation);

                // If there's an initial message, send it
                if (!string.IsNullOrWhiteSpace(dto.InitialMessage))
                {
                    await _chatService.SendMessageAsync(createdConversation.Id, userId, dto.InitialMessage, senderType);

                    // Update conversation's last message time
                    createdConversation.LastMessageAt = DateTime.UtcNow;
                    createdConversation.TotalMessages += 1;
                }

                // CRITICAL: Reload conversation with all navigation properties for proper DTO mapping
                var fullConversation = await _chatService.GetConversationByIdAsync(createdConversation.Id);
                var conversationDto = _mapper.Map<ChatConversationDto>(fullConversation);

                return Success(conversationDto, "Conversation created successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to create conversation: {ex.Message}", 500);
            }
        }

        #endregion

        #region Conversation Actions

        /// <summary>
        /// Assign conversation to agent (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("conversations/{id:int}/assign")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> AssignConversation(int id, [FromBody] AssignConversationDto dto)
        {
            try
            {
                var agentId = dto.AgentId ?? GetCurrentUserId();
                var conversation = await _chatService.AssignConversationAsync(id, agentId);
                var conversationDto = _mapper.Map<ChatConversationDto>(conversation);
                return Success(conversationDto, "Conversation assigned successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to assign conversation: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Close conversation (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("conversations/{id:int}/close")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> CloseConversation(int id, [FromBody] CloseConversationDto dto)
        {
            try
            {
                var conversation = await _chatService.CloseConversationAsync(id, dto.ResolutionNotes);
                var conversationDto = _mapper.Map<ChatConversationDto>(conversation);
                return Success(conversationDto, "Conversation closed successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to close conversation: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Reopen conversation (Customer or Admin)
        /// </summary>
        [HttpPost("conversations/{id:int}/reopen")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> ReopenConversation(int id)
        {
            try
            {
                var conversation = await _chatService.ReopenConversationAsync(id);
                var conversationDto = _mapper.Map<ChatConversationDto>(conversation);
                return Success(conversationDto, "Conversation reopened successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to reopen conversation: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Transfer conversation to another agent (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("conversations/{id:int}/transfer")]
        public async Task<ActionResult<ApiResponse<ChatConversationDto>>> TransferConversation(int id, [FromBody] TransferConversationDto dto)
        {
            try
            {
                var conversation = await _chatService.TransferConversationAsync(id, dto.NewAgentId);
                var conversationDto = _mapper.Map<ChatConversationDto>(conversation);
                return Success(conversationDto, "Conversation transferred successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatConversationDto>($"Failed to transfer conversation: {ex.Message}", 500);
            }
        }

        #endregion

        #region Messages

        /// <summary>
        /// Get conversation messages
        /// </summary>
        [HttpGet("conversations/{id:int}/messages")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ChatMessageDto>>>> GetConversationMessages(int id)
        {
            try
            {
                var messages = await _chatService.GetConversationMessagesAsync(id);
                var messageDtos = _mapper.Map<IEnumerable<ChatMessageDto>>(messages);
                return Success(messageDtos, "Messages retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ChatMessageDto>>($"Failed to retrieve messages: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Send message
        /// </summary>
        [HttpPost("conversations/{id:int}/messages")]
        public async Task<ActionResult<ApiResponse<ChatMessageDto>>> SendMessage(int id, [FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                // FIXED: Properly determine sender type based on role
                string senderType;
                if (User.IsInRole("Customer"))
                {
                    senderType = "Customer";
                }
                else if (User.IsInRole("HallManager"))
                {
                    senderType = "HallManager";
                }
                else if (User.IsInRole("VendorManager"))
                {
                    senderType = "VendorManager";
                }
                else if (User.IsInRole("Admin"))
                {
                    senderType = "Admin";
                }
                else
                {
                    senderType = "User"; // Fallback
                }

                var message = await _chatService.SendMessageAsync(id, userId, dto.Message, senderType);
                var messageDto = _mapper.Map<ChatMessageDto>(message);
                return Success(messageDto, "Message sent successfully");
            }
            catch (Exception ex)
            {
                return Error<ChatMessageDto>($"Failed to send message: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        [HttpPost("conversations/{id:int}/mark-read")]
        public async Task<ActionResult<ApiResponse<string>>> MarkMessagesAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatService.MarkMessagesAsReadAsync(id, userId);
                return Success<string>("Messages marked as read successfully", "Messages marked as read");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to mark messages as read: {ex.Message}", 500);
            }
        }

        #endregion

        #region Rating

        /// <summary>
        /// Rate conversation and employee (Customer only)
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPost("conversations/{id:int}/rate")]
        public async Task<ActionResult<ApiResponse<string>>> RateConversation(int id, [FromBody] RateConversationDto dto)
        {
            try
            {
                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    return Error<string>("Rating must be between 1 and 5", 400);
                }

                var result = await _chatService.RateConversationAsync(id, dto.Rating, dto.Feedback);
                if (!result)
                {
                    return Error<string>("Failed to rate conversation", 500);
                }

                return Success<string>($"Thank you for your feedback! You rated this conversation {dto.Rating}/5 stars.", "Rating submitted successfully");
            }
            catch (Exception ex)
            {
                return Error<string>($"Failed to rate conversation: {ex.Message}", 500);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get chat statistics (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetChatStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var statistics = await _chatService.GetChatStatisticsAsync(from, to);
                return Success(statistics, "Statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve statistics: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get agent performance (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("agents/{agentId:int}/performance")]
        public async Task<ActionResult<ApiResponse<object>>> GetAgentPerformance(int agentId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var performance = await _chatService.GetAgentPerformanceAsync(agentId, from, to);
                return Success(performance, "Agent performance retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Failed to retrieve agent performance: {ex.Message}", 500);
            }
        }

        #endregion
    }
}
