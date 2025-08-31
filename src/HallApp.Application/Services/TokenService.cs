using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HallApp.Core.Entities;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HallApp.Application.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;
    private readonly UserManager<AppUser> _userManager;

    public TokenService(IConfiguration config, UserManager<AppUser> userManager)
    {
        _config = config;
        _userManager = userManager;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["TokenKey"]));
    }

    // Method to create a token
    public async Task<string> CreateToken(AppUser user)
    {
        var claims = new List<Claim>{
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // Add for SignalR
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())  // Standard subject claim
        };

        // Add roles to claims
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddHours(1),  // Token expiry time - shorter for access tokens
            SigningCredentials = creds,
            Audience = _config["JWT:Audience"] ?? "hallbookingapp",
            Issuer = _config["JWT:Issuer"] ?? "hallbookingapi"
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);  // Return the generated JWT token
    }

    // Method to create a refresh token
    public Task<string> CreateRefreshToken(AppUser user)
    {
        var claims = new List<Claim>{
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim("TokenType", "RefreshToken")
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(7),  // Refresh tokens last longer
            SigningCredentials = creds,
            Audience = _config["JWT:Audience"] ?? "hallbookingapp",
            Issuer = _config["JWT:Issuer"] ?? "hallbookingapi"
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    // Method to validate a refresh token
    public Task<ClaimsPrincipal> ValidateRefreshToken(string refreshToken)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = true  // We validate lifetime for refresh tokens
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            // Validate the token and extract the claims
            var principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out SecurityToken validatedToken);

            // Ensure that the token is a valid JWT token
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            // Verify this is a refresh token
            var tokenTypeClaim = principal.FindFirst("TokenType");
            if (tokenTypeClaim == null || tokenTypeClaim.Value != "RefreshToken")
            {
                throw new SecurityTokenException("Not a refresh token");
            }

            return Task.FromResult(principal);
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Invalid refresh token", ex);
        }
    }

    // Method to extract claims from an expired token
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // You can enable this if you want to validate the audience
            ValidateIssuer = false,   // You can enable this if you want to validate the issuer
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,  // Use the same key to validate the token
            ValidateLifetime = false  // We are ignoring token expiration time in this method
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            // Validate the token and extract the claims
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

            // Ensure that the token is a valid JWT token
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;  // Return the claims principal
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException("Invalid token", ex);
        }
    }
}
