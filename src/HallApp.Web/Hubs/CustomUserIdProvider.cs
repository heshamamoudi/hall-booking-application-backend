using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace HallApp.Web.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Use same claim fallback logic as NotificationHub for consistency
        var userIdClaim = connection.User?.FindFirst(ClaimTypes.NameIdentifier)
                         ?? connection.User?.FindFirst(JwtRegisteredClaimNames.NameId)
                         ?? connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? connection.User?.FindFirst("nameid")
                         ?? connection.User?.FindFirst("sub");

        return userIdClaim?.Value;
    }
}
