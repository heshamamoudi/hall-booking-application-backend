using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Debug all available claims
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine("🔍 ChatHub User Claims:");
            foreach (var claim in Context.User.Claims)
            {
                Console.WriteLine($"   {claim.Type}: {claim.Value}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ ChatHub: User not authenticated or claims not available");
        }

        // Get user ID from claims with multiple fallbacks
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? Context.User?.FindFirst("nameid")
                         ?? Context.User?.FindFirst("sub");

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            Console.WriteLine("Warning: User ID not found in claims. ChatHub disabled for this connection.");
            await base.OnConnectedAsync();
            return;
        }

        var userId = userIdClaim.Value;
        Console.WriteLine($"✅ ChatHub User connected with UserID: {userId}");

        // Add user to their personal group for direct messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Join a specific conversation group
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        var groupName = $"conversation_{conversationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"💬 User joined conversation group: {groupName}");
    }

    /// <summary>
    /// Leave a specific conversation group
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        var groupName = $"conversation_{conversationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"👋 User left conversation group: {groupName}");
    }

    /// <summary>
    /// Send typing indicator to conversation
    /// </summary>
    public async Task SendTypingIndicator(int conversationId, string userName)
    {
        var groupName = $"conversation_{conversationId}";
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            ConversationId = conversationId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast new message to conversation participants
    /// </summary>
    public async Task SendMessageToConversation(int conversationId, object message)
    {
        var groupName = $"conversation_{conversationId}";
        await Clients.Group(groupName).SendAsync("ReceiveMessage", message);
        Console.WriteLine($"📨 Message sent to conversation {conversationId}");
    }

    /// <summary>
    /// Send message read notification
    /// </summary>
    public async Task SendMessageReadNotification(int conversationId, int userId)
    {
        var groupName = $"conversation_{conversationId}";
        await Clients.OthersInGroup(groupName).SendAsync("MessagesRead", new
        {
            ConversationId = conversationId,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }
}
