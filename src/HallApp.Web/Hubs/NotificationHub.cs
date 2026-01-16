using HallApp.Core.Entities.NotificationEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

public class NotificationHub : Hub
{
    // When a client connects, add them to their customer group
    public override async Task OnConnectedAsync()
    {
        // Debug all available claims
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine("🔍 SignalR User Claims:");
            foreach (var claim in Context.User.Claims)
            {
                Console.WriteLine($"   {claim.Type}: {claim.Value}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ SignalR: User not authenticated or claims not available");
        }

        // Get user ID from claims with multiple fallbacks
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) 
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? Context.User?.FindFirst("nameid")
                         ?? Context.User?.FindFirst("sub");

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            Console.WriteLine("Warning: User ID not found in claims. SignalR notifications disabled for this connection.");
            // Still continue with base connection for non-authenticated features
            await base.OnConnectedAsync();
            return;
        }

        var userId = userIdClaim.Value;
        Console.WriteLine($"✅ SignalR User connected with UserID: {userId}");
        
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    // Client-callable method for explicitly joining user group
    public async Task JoinUserGroup()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? Context.User?.FindFirst("nameid")
                         ?? Context.User?.FindFirst("sub");

        if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value))
        {
            var userId = userIdClaim.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"👥 User {userId} explicitly joined notification group");
        }
        else
        {
            Console.WriteLine("⚠️ JoinUserGroup called but no valid user ID found in claims");
        }
    }

    // Method to send a notification to a specific user (based on connection ID)
    public async Task SendNotificationToUser(int userId, Notification notification)
    {
        // Send the notification to the specific user group
        await Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }
}
