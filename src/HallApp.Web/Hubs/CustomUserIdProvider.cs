using Microsoft.AspNetCore.SignalR;

namespace HallApp.Web.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Map the UserIdentifier to the "nameid" claim
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}
