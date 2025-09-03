using AutoMapper;
using HallApp.Core.Entities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Identity;

namespace HallApp.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<AppUser> _userManager;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<AppUser> CreateUserAsync(AppUser user)
    {
        var createdUser = await _unitOfWork.UserRepository.CreateUser(user);
        await _unitOfWork.Complete();
        return createdUser;
    }

    public async Task<AppUser> UpdateUserAsync(AppUser user)
    {
        var updatedUser = await _unitOfWork.UserRepository.UpdateUser(user);
        await _unitOfWork.Complete();
        return updatedUser;
    }

    public async Task<bool> DeleteHallManagerAsync(string userId)
    {
        var result = await _unitOfWork.UserRepository.DeleteUser(userId);
        await _unitOfWork.Complete();
        return result;
    }

    public async Task<List<AppUser>> GetHallManagersAsync()
    {
        var managers = await _unitOfWork.UserRepository.GetHallManagersAsync();
        return managers.ToList();
    }

    public async Task<AppUser> GetUserByIdAsync(string userId)
    {
        return await _unitOfWork.UserRepository.FindByIdAsync(userId);
    }

    public async Task<List<AppUser>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.UserRepository.GetUsersAsync();
        return users.ToList();
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string roleName)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, roleName);
        
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<bool> ValidateUserPermissionsAsync(string userId, string permission)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        var roles = await _userManager.GetRolesAsync(user);
        
        // Basic permission validation based on roles
        // This can be expanded based on specific business requirements
        return roles.Contains("Admin") || roles.Contains("HallManager") || roles.Contains("VendorManager");
    }

    public async Task<AppUser> CreateHallManagerAsync(AppUser hallManager)
    {
        var result = await _userManager.CreateAsync(hallManager);
        
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(hallManager, "HallManager");
        await _unitOfWork.Complete();
        return hallManager;
    }

    public async Task<AppUser> UpdateHallManagerAsync(string userId, AppUser userData)
    {
        var existingUser = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (existingUser == null) return new AppUser();

        // Update user properties
        existingUser.FirstName = userData.FirstName;
        existingUser.LastName = userData.LastName;
        existingUser.Email = userData.Email;
        existingUser.PhoneNumber = userData.PhoneNumber;
        
        var updatedUser = await _unitOfWork.UserRepository.UpdateUser(existingUser);
        await _unitOfWork.Complete();
        return updatedUser;
    }

    public async Task<List<AppUser>> GetUsersByRoleAsync(string roleName)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        return usersInRole.ToList();
    }

    public async Task<bool> DeactivateUserAsync(string userId)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        user.Active = false;
        await _userManager.UpdateAsync(user);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> ReactivateUserAsync(string userId)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        user.Active = true;
        await _userManager.UpdateAsync(user);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> ResetUserPasswordAsync(string userId, string newPassword)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        
        if (result.Succeeded)
        {
            await _unitOfWork.Complete();
            return true;
        }
        return false;
    }

    public async Task<bool> SendUserInvitationAsync(string email, string role)
    {
        // Check if user already exists
        var existingUser = await _unitOfWork.UserRepository.FindByEmailAsync(email);
        if (existingUser != null) return false;

        // Create invitation record or send email notification
        // This would typically involve an email service
        // For now, we'll just return true as placeholder for email functionality
        await Task.CompletedTask;
        return true;
    }

    public async Task<List<AppUser>> SearchUsersAsync(string searchTerm)
    {
        var allUsers = await _unitOfWork.UserRepository.GetUsersAsync();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
            return allUsers.ToList();
            
        return allUsers.Where(u => 
            (u.FirstName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.LastName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (u.UserName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();
    }

    public async Task<AppUser> UpdateUserProfileAsync(string userId, AppUser profileData)
    {
        var existingUser = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (existingUser == null) return new AppUser();

        // Update profile properties
        existingUser.FirstName = profileData.FirstName;
        existingUser.LastName = profileData.LastName;
        existingUser.Email = profileData.Email;
        existingUser.PhoneNumber = profileData.PhoneNumber;
        existingUser.Gender = profileData.Gender;
        
        var updatedUser = await _unitOfWork.UserRepository.UpdateUser(existingUser);
        await _unitOfWork.Complete();
        return updatedUser;
    }

    public async Task<AppUser> GetUserProfileAsync(string userId)
    {
        return await _unitOfWork.UserRepository.FindByIdAsync(userId);
    }

    public async Task<List<AppUser>> GetUsersRequiringApprovalAsync()
    {
        var allUsers = await _unitOfWork.UserRepository.GetUsersAsync();
        // Filter users who need approval (assuming there's an IsApproved property)
        return allUsers.Where(u => !u.Active).ToList();
    }

    public async Task<bool> ApproveUserAsync(string userId)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        user.Active = true;
        await _userManager.UpdateAsync(user);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> RejectUserAsync(string userId, string reason)
    {
        var user = await _unitOfWork.UserRepository.FindByIdAsync(userId);
        if (user == null) return false;

        // For rejection, we could either delete the user or mark them as rejected
        // Here we'll delete the user account
        var result = await _userManager.DeleteAsync(user);
        await _unitOfWork.Complete();
        return result.Succeeded;
    }
}
