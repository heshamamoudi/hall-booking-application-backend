using HallApp.Core.Entities;
using System.Security.Claims;

namespace HallApp.Core.Interfaces.IServices;

public interface ITokenService
{
    Task<string> CreateToken(AppUser user);
    Task<string> CreateRefreshToken(AppUser user);
    Task<ClaimsPrincipal> ValidateRefreshToken(string refreshToken);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
