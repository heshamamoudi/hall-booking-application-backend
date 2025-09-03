using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface IUserService
    {
        Task<AppUser> CreateUserAsync(AppUser user);
        Task<AppUser> UpdateUserAsync(AppUser user);
        Task<bool> DeleteHallManagerAsync(string userId);
        Task<List<AppUser>> GetHallManagersAsync();
        Task<AppUser> GetUserByIdAsync(string userId);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(string userId, string roleName);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<bool> ValidateUserPermissionsAsync(string userId, string permission);
        Task<AppUser> CreateHallManagerAsync(AppUser hallManager);
        Task<AppUser> UpdateHallManagerAsync(string userId, AppUser userData);
        Task<List<AppUser>> GetUsersByRoleAsync(string roleName);
        Task<bool> DeactivateUserAsync(string userId);
        Task<bool> ReactivateUserAsync(string userId);
        Task<bool> ResetUserPasswordAsync(string userId, string newPassword);
        Task<bool> SendUserInvitationAsync(string email, string role);
        Task<List<AppUser>> SearchUsersAsync(string searchTerm);
        Task<AppUser> UpdateUserProfileAsync(string userId, AppUser profileData);
        Task<AppUser> GetUserProfileAsync(string userId);
        Task<List<AppUser>> GetUsersRequiringApprovalAsync();
        Task<bool> ApproveUserAsync(string userId);
        Task<bool> RejectUserAsync(string userId, string reason);
    }
}
