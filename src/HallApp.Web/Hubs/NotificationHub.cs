using HallApp.Core.Entities.NotificationEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

public class NotificationHub : Hub
{
    // When a client connects, add them to their customer group
    [Authorize]
    public override async Task OnConnectedAsync()
    {
        // Get user ID from claims instead of Context.UserIdentifier
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) 
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
        {
            Console.WriteLine("Warning: User ID not found in claims. SignalR notifications disabled for this connection.");
            return; // Don't throw error, just skip group assignment
        }

        var userId = userIdClaim.Value;
        Console.WriteLine($"User connected with UserID: {userId}");
        
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    // Method to send a notification to a specific user (based on connection ID)
    public async Task SendNotificationToUser(int userId, Notification notification)
    {
        // Send the notification to the specific user group
        await Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }
}
