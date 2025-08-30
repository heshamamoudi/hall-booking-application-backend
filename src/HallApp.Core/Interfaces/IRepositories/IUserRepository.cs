using HallApp.Core.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IUserRepository
{
    // User management
    Task<IEnumerable<AppUser>> GetUsersAsync();
    Task<AppUser> GetUserByPhoneAsync(string phone);
    Task<AppUser> GetUserByEmailAsync(string email);
    Task<AppUser> GetUserByUsernameAsync(string username);
    Task<AppUser> GetUserById(int userId);
    Task<AppUser> FindByIdAsync(string userId);
    Task<AppUser> FindByEmailAsync(string email);
    Task<AppUser> FindByUsernameAsync(string username);

    // User creation
    Task<AppUser> CreateUser(AppUser user);
    Task<AppUser> UpdateUser(AppUser user);
    Task<bool> DeleteUser(string userId);
    Task<IEnumerable<AppUser>> GetHallManagersAsync();

    // Authentication and validation
    Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password);
    Task<AppUser> AuthenticateUser(string login, string password, bool isVendorManager);
    Task<bool> UserExists(string email, string phoneNumber);
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    // Token management
    Task<string> GenerateTokenAsync(AppUser user);
    Task<string> GenerateRefreshTokenAsync(AppUser user);
    Task RegisterOrUpdateTokenAsync(AppUser user, string token, string refreshToken);
    Task<ClaimsPrincipal> ValidateRefreshTokenAsync(string refreshToken);
    Task UpdateSecurityStampAsync(AppUser user);
}
