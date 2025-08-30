using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Application.Services;

/// <summary>
/// Combined service for operations requiring both AppUser + VendorManager data
/// Used for vendor profile management, vendor dashboard, authentication-related operations
/// </summary>
public class VendorProfileService : IVendorProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IVendorManagerService _vendorManagerService;
    private readonly IUserService _userService;

    public VendorProfileService(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IVendorManagerService vendorManagerService,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _vendorManagerService = vendorManagerService;
        _userService = userService;
    }

    // Profile operations (combines AppUser + VendorManager)
    public async Task<(AppUser AppUser, VendorManager VendorManager)?> GetVendorProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return null;

        // Get AppUser data
        var appUser = await _userService.GetUserByIdAsync(userId);
        if (appUser == null) return null;

        // Get VendorManager data
        var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userIdInt);
        if (vendorManager == null) return null;

        return (appUser, vendorManager);
    }

    public async Task<bool> UpdateVendorProfileAsync(string userId, AppUser userData, VendorManager vendorManagerData)
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

        // Update VendorManager data through VendorManagerService
        var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userIdInt);
        if (vendorManager == null) return false;

        vendorManager.CommercialRegistrationNumber = vendorManagerData.CommercialRegistrationNumber;
        vendorManager.VatNumber = vendorManagerData.VatNumber;
        vendorManager.IsApproved = vendorManagerData.IsApproved;

        await _vendorManagerService.UpdateVendorManagerAsync(vendorManager);
        return true;
    }

    public async Task<bool> DeleteVendorProfileAsync(string userId)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        // Get vendor manager first
        var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userIdInt);
        if (vendorManager == null) return false;

        // Delete vendor manager (business data)
        var vendorManagerDeleted = await _vendorManagerService.DeleteVendorManagerAsync(vendorManager.Id);
        if (!vendorManagerDeleted) return false;

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
        return await _userService.SendUserInvitationAsync(userId, "VendorManager");
    }

    // Business profile operations
    public async Task<(AppUser AppUser, VendorManager VendorManager)?> GetVendorDashboardAsync(string userId)
    {
        var profile = await GetVendorProfileAsync(userId);
        if (profile == null) return null;

        // Load additional dashboard data if needed
        return profile;
    }

    public async Task<bool> UpdateBusinessPreferencesAsync(string userId, VendorManager businessData)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userIdInt);
        if (vendorManager == null) return false;

        // Update business-specific preferences
        vendorManager.CommercialRegistrationNumber = businessData.CommercialRegistrationNumber;
        vendorManager.VatNumber = businessData.VatNumber;
        vendorManager.IsApproved = businessData.IsApproved;

        var updatedVendorManager = await _vendorManagerService.UpdateVendorManagerAsync(vendorManager);
        return updatedVendorManager != null;
    }

    public async Task<bool> ApproveVendorManagerAsync(string userId, bool isApproved)
    {
        if (!int.TryParse(userId, out int userIdInt))
            return false;

        var vendorManager = await _vendorManagerService.GetVendorManagerByAppUserIdAsync(userIdInt);
        if (vendorManager == null) return false;

        return await _vendorManagerService.ApproveVendorManagerAsync(vendorManager.Id, isApproved);
    }
}
