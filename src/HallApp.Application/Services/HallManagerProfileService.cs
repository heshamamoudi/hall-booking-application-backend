using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Combined service for operations requiring both AppUser + HallManager data
/// Used for hall manager profile management, hall manager dashboard, authentication-related operations
/// </summary>
public class HallManagerProfileService : IHallManagerProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHallManagerService _hallManagerService;
    private readonly IUserService _userService;

    public HallManagerProfileService(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IHallManagerService hallManagerService,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _hallManagerService = hallManagerService;
        _userService = userService;
    }

    // Profile operations (combines AppUser + HallManager)
    public async Task<(AppUser AppUser, HallManager HallManager)?> GetHallManagerProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return null;

        // Get AppUser data
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return null;

        // Get HallManager data
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userIdInt);
        if (hallManager == null) return null;

        return (appUser, hallManager);
    }

    public async Task<bool> UpdateHallManagerProfileAsync(string userId, AppUser userData, HallManager hallManagerData)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        // Update AppUser data through UserService
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return false;

        appUser.FirstName = userData.FirstName;
        appUser.LastName = userData.LastName;
        appUser.PhoneNumber = userData.PhoneNumber;
        appUser.Updated = DateTime.UtcNow;

        await _userService.UpdateUserAsync(appUser);

        // Update HallManager data through HallManagerService
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userIdInt);
        if (hallManager == null) return false;

        hallManager.CompanyName = hallManagerData.CompanyName;
        hallManager.CommercialRegistrationNumber = hallManagerData.CommercialRegistrationNumber;
        hallManager.IsApproved = hallManagerData.IsApproved;

        await _hallManagerService.UpdateHallManagerAsync(hallManager);
        return true;
    }

    public async Task<bool> DeleteHallManagerProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        // Get hall manager first
        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userIdInt);
        if (hallManager == null) return false;

        // Delete hall manager (business data)
        var hallManagerDeleted = await _hallManagerService.DeleteHallManagerAsync(hallManager.Id);
        if (!hallManagerDeleted) return false;

        // Delete user (auth data) - handled by UserService
        return await _userService.DeleteHallManagerAsync(userId);
    }

    // Authentication-related profile operations
    public async Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        // Using ResetUserPasswordAsync as available method
        return await _userService.ResetUserPasswordAsync(userId, newPassword);
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token)
    {
        // Email verification logic would be implemented here
        // For now, return true as placeholder
        return await Task.FromResult(true);
    }

    public async Task<bool> SendVerificationEmailAsync(string userId)
    {
        // Implementation would generate token and send email
        return await _userService.SendUserInvitationAsync(userId, "HallManager");
    }

    // Business profile operations
    public async Task<(AppUser AppUser, HallManager HallManager)?> GetHallManagerDashboardAsync(string userId)
    {
        var profile = await GetHallManagerProfileAsync(userId);
        if (profile == null) return null;

        // Load additional dashboard data if needed
        return profile;
    }

    public async Task<bool> UpdateBusinessPreferencesAsync(string userId, HallManager businessData)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userIdInt);
        if (hallManager == null) return false;

        // Update business-specific preferences
        hallManager.CompanyName = businessData.CompanyName;
        hallManager.CommercialRegistrationNumber = businessData.CommercialRegistrationNumber;
        hallManager.IsApproved = businessData.IsApproved;

        var updatedHallManager = await _hallManagerService.UpdateHallManagerAsync(hallManager);
        return updatedHallManager != null;
    }

    public async Task<bool> ApproveHallManagerAsync(string userId, bool isApproved)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        var hallManager = await _hallManagerService.GetHallManagerByAppUserIdAsync(userIdInt);
        if (hallManager == null) return false;

        return await _hallManagerService.ApproveHallManagerAsync(hallManager.Id, isApproved);
    }
}
