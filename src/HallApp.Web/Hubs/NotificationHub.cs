using HallApp.Core.Entities.NotificationEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HallApp.Web.Hubs;

public class NotificationHub : Hub
{
    // When a client connects, add them to their customer group
    [Authorize]
    public override async Task OnConnectedAsync()
    {
        // Log claims for debugging
        var claims = Context.User?.Claims;
        if (claims != null)
        {
            Console.WriteLine("JWT Claims:");
            foreach (var claim in claims)
            {
                Console.WriteLine($"Type: {claim.Type}, Value: {claim.Value}");
            }
        }

        var userId = Context.UserIdentifier; // Should now be populated
        Console.WriteLine($"UserIdentifier: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("Error: UserIdentifier is null or empty.");
            throw new ArgumentException("UserIdentifier is null or empty");
        }

        Console.WriteLine($"User connected with UserIdentifier: {userId}");
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
